try:
    import decimal
except:
    print("IMPORT ERROR: Could not import decimal")
try:
    import sys
except:
    print("IMPORT ERROR: Could not import sys")
try:
    from pypdf import PdfReader  # type: ignore
except:
    print("IMPORT ERROR: Could not import PdfReader from pypdf")
try:
    import re
except:
    print("IMPORT ERROR: Could not import re")
try:
    import json
except:
    print("IMPORT ERROR: Could not import json")
try:
    from json import JSONEncoder
except:
    print("IMPORT ERROR: Could not import JSONEncoder from json")

class Trip:
    def __init__(self, itinerary_num, starting_fuel, aircraft, legs, minimum_fuel_reserve, max_fuel_reserve, max_takeoff, max_landing, basic_empty_weight, errors, penalty, lbs_per_hour):
        self.itinerary_num = itinerary_num
        self.starting_fuel = starting_fuel
        self.aircraft = aircraft
        self.legs = legs
        self.minimum_fuel_reserve = minimum_fuel_reserve
        self.max_fuel_reserve = max_fuel_reserve
        self.max_takeoff_weight = max_takeoff
        self.max_landing_weight = max_landing
        self.basic_empty_weight = basic_empty_weight
        self.penalty = penalty
        self.lbs_per_hour = lbs_per_hour
        self.errors = errors
    
    @classmethod
    def from_json(cls, json_string):
        json_dict = json.loads(json_string)
        return cls(**json_dict)

class Leg:
    def __init__(self, leg_num, departure, destination, arrival_fuel_price, departure_fuel_price, fees, reserve, fuel_burn, passengers, baggage, distance, crew_weight, max_takeoff_weight, max_landing_weight, max_ramp_weight, taxi_fuel_burn): 
        self.departure = departure
        self.destination = destination
        self.arrival_fuel_price = arrival_fuel_price
        self.departure_fuel_price = departure_fuel_price
        self.fees = fees
        self.reserve = reserve
        self.fuel_burn = fuel_burn
        self.passengers = passengers
        self.baggage = baggage
        self.leg_num = leg_num
        self.distance = distance
        self.crew_weight = crew_weight
        self.max_takeoff_weight = max_takeoff_weight
        self.max_landing_weight = max_landing_weight
        self.max_ramp_weight = max_ramp_weight
        self.taxi_fuel_burn = taxi_fuel_burn

    
    def get_total_carrying_weight(self):
        total = sum(self.crew_weight) + sum(self.passengers) + self.baggage
        return total

class Fee: 
    def __init__(self, name, amount, is_waivable, waived_at, airport):
        self.name = name
        self.amount = amount
        self.is_waivable = is_waivable
        self.waived_at = waived_at
        self.airport = airport
class Fuel_Price:
    def __init__(self, min_fuel, price):
        self.min_fuel = min_fuel
        self.price = price

class TripEncoder(JSONEncoder):
        def default(self, o):
            return o.__dict__
class TripDecoder(json.JSONDecoder):
    def __init__(self, *args, **kwargs):
        json.JSONDecoder.__init__(self, object_hook=self.object_hook, *args, **kwargs)
    
    def object_hook(self, obj):
        if("min_fuel" in obj):
            return Fuel_Price(obj['min_fuel'], obj['price'])
        elif("arrival_fuel_price" in obj):
            return Leg(obj['leg_num'], obj['departure'], obj['destination'], obj['arrival_fuel_price'], obj['departure_fuel_price'],
                       obj['fees'], obj['reserve'], obj['fuel_burn'], obj['passengers'], obj['baggage'], obj['distance'], obj['crew_weight'], obj['max_takeoff_weight'], obj['max_landing_weight'], obj['max_ramp_weight'], obj['taxi_fuel_burn'])
        elif("is_waivable" in obj):
            return Fee(obj['name'], obj['amount'], obj['is_waivable'], obj['waived_at'], obj['airport'])
        else:
            return Trip(obj["itinerary_num"], obj['starting_fuel'], obj['aircraft'], obj['legs'], obj['minimum_fuel_reserve'], obj['max_fuel_reserve'], obj['max_takeoff_weight'], 
                        obj['max_landing_weight'], obj['basic_empty_weight'], obj['errors'], obj['penalty'], obj['lbs_per_hour'])
        

def extract_all_lines(filename):    
    reader = PdfReader(filename)
    allLines = ""
    for i in range(reader.get_num_pages()):        
        extracted = str(reader.pages[i].extract_text())
        allLines += extracted.encode("ascii", "ignore").decode()
    return allLines

def get_fees(fee_str):
    split_char = ""
    if "//" in fee_str:
        split_char = "//"
    elif "," in fee_str and ",waived" not in fee_str:
        split_char = ","
    elif "/" in fee_str and "ww/" not in fee_str: 
        split_char = "/"
    else: 
        split_char = "#"
    fees_arr = fee_str.split(split_char)
    
    fees = []
    for f in fees_arr:
        if "ww" in f or "waivedwith" in f:     
            waived_text = "ww"
            if "ww/" in f:
                waived_text = "ww/"
            elif ",waivedwith" in f:
                waived_text = ",waivedwith"
            elif "waivedwith" in f:
                waived_text = "waivedwith"
            if "$" in f:
                price_idx = f.index("$")
                middle_idx = f.index(waived_text)
                if f[price_idx+1:middle_idx].strip() != '':
                    price = float(f[price_idx+1:middle_idx].strip())
                    min_gals = f[middle_idx + len(waived_text):len(f)]
                    if "g" in min_gals:
                        min_gals = float(min_gals[0:min_gals.index("g")].strip())
                    elif "L" in min_gals or "l" in min_gals:
                        min_gals = float(min_gals[0:min_gals.lower().index("l")].strip())/3.785
                    fee = Fee("Waived", price, True, min_gals, "")
                    fees.append(fee)
                    #TODO: ERROR HANDLING EX 20: 1nightswaivedwith0gals/Groundhandlingfee:$450 price = float(f[price_idx+1:middle_idx].strip()) ValueError: could not convert string to float: ''
            #TODO: ERROR HANDLING EX 17: 1nightswaivedwith0gals/Groundhandlingfee:  price_idx = f.index("$") ValueError: substring not found
        else : 
            stripped = re.sub(r'[^0-9.$]',' ', f).strip()            
            stripped_arr = stripped.split(" ")
            for s in stripped_arr:
                name = "Other"
                amount = 0
                is_waivable = False
                waived_at = 0
                if "$" in s:
                    s = s.strip()
                    amount = float(s[1:len(s)])
                    if amount > 0:
                        fee = Fee("Other", amount, False, 0, "")
                        fees.append(fee)
    return fees


def read_doc(file):
    reader = PdfReader(file)
    pages = reader.get_num_pages()
    plane = ""
    legs = []
    itinerary_num = ""
    errors = []
    #print("C:/Users/Lanex/Desktop/example" + page_num + ".pdf")

    try:
        extracted = reader.pages[0].extract_text()
        lines_1 = extracted.splitlines()
        extra_leg = False

        for i in range(len(lines_1)):
            line = lines_1[i].replace(" ", "")
            if line.startswith("CrewItinerary"):
                itinerary_num = line[line.index("(") + 1:line.index(")")]
                
            elif len(plane) == 0 and line.startswith("Leg") and ")" in line and not line.endswith(")"):
                plane = line[line.rindex(")") + 2 : len(line)-1]
                
            elif len(plane) == 0 and len(line) > 3 and line[1] == " " and not line.strip().endswith(":"):   
                plane = line

            if len(plane) > 0:
                plane = plane.strip().replace(" ", "")
                plane = plane[0:6]
                m = re.match("[N][0-9]{1,5}[A-Z]{1,2}", plane)#.group(0)
                if m:
                    plane = m.group(0)
            if line.startswith("Leg") and "Pax" not in line: 
                extra_leg = True

        lines_remaining = []
        for i in range(0 if extra_leg else 1, pages):
            page = reader.pages[i]
            
            extracted = page.extract_text(extraction_mode="layout")

            lines = extracted.splitlines()
            for line in lines:
                lines_remaining.append(line.encode("ascii", "ignore").decode().replace(" ", "").strip())

        num_lges_on_page = sum(1 for i in lines_remaining if i.startswith("Leg") and "Distance" in i)
        legs_indicies = [lines_remaining.index(i) for i in lines_remaining if i.startswith("Leg") and "Distance" in i]

        for i in range(len(legs_indicies)):
            idx_start = legs_indicies[i]
            idx_end = legs_indicies[i+1] if i != len(legs_indicies) -1 else len(lines_remaining)
            
            departure = ""
            destination = ""
            arrival_fuel_price = ""
            departure_fuel_price = []
            fees = []
            reserve = 1000
            fuel_burn = 0
            pax_listed = 0
            passengers = []
            other = []
            baggage = 0
            leg_num = 0
            distance = 0
            crew = []
            
            in_other = False
            for j in range(idx_start, idx_end):
                line = lines_remaining[j]

                if line.startswith("Leg") and "Distance" in line:
                    digits = 1
                    if i > 8: 
                        digits = 2
                    
                    idx_leg = line.index("Leg") + 4
                    leg_num = line[idx_leg:idx_leg + digits]
                    
                    idx_dist = line.index("Distance") + len("Distance: ")
                    idx_dist_end = line.find("nm", idx_dist)
                    distance = line[idx_dist:idx_dist_end].strip()

                elif line.startswith("DEPARTS:"):
                    idx_dept_start = len("DEPARTS:")        
                    idx_dept_end = line.index("-",)
                    departure = line[idx_dept_start:idx_dept_end].strip()

                    idx_arr_start = line.index("ARRIVES")
                    idx_arr_end = line.find("-", idx_arr_start)
                    destination = line[idx_arr_start + len("ARRIVES:"):idx_arr_end].strip().replace(" ", "")

                elif line.startswith("Crew") and "PIC:" in line:
                    has_sic = "SIC:" in lines_remaining[j + 1]
                    crew.append(180)
                    if has_sic:
                        crew.append(180)
                    baggage += 100 if has_sic else 50

                    j = j + 1 if has_sic else j
                elif line.startswith("Fuel(" + departure + ")") or line.startswith("Fuel(" + departure + " )") :
                    prices = line.split("/")
                    for fuel_price in prices:
                        if "$" in fuel_price:
                            fuel_price_info = fuel_price.split(":")
                            fuel_price_min_idx = 0
                            fuel_price_price_idx = 1

                            if re.search('[a-zA-Z]', fuel_price_info[0]):
                                fuel_price_min_idx = 1
                                fuel_price_price_idx = 2
                            fuel_price_min = fuel_price_info[fuel_price_min_idx]
                            if "+" in fuel_price_min:
                                fuel_price_min = int(fuel_price_min[0:fuel_price_min.index("+")].strip())
                            else:
                                fuel_price_min = 0                        
                            
                            fuel_price_price = fuel_price_info[fuel_price_price_idx]
                            fuel_price_price = float(fuel_price_price[fuel_price_price.index("$")+1:len(fuel_price_price)])

                            departure_fuel_price.append(Fuel_Price(fuel_price_min, fuel_price_price))
                    if len(departure_fuel_price) == 0:
                        errors.append("LEG " + leg_num + " FUEL ERROR: Could not find fuel price on line '" + line + "'")
                elif line.startswith("FBO:") and "$" in line:
                    fees.extend(get_fees(line[len("FBO:"):len(line)]))            
                elif (line.startswith("Fees(")):
                    idx_fees_start = line.index(":")
                    line_fees = line[idx_fees_start + 1:len(line)]
                    fees.extend(get_fees(line_fees))
                elif line.startswith("Pax("):
                    in_other = False
                    try:
                        num_pax = int(line[len("Pax("):line.index(")")].strip())   
                        for p in range(num_pax):
                            p_line = lines_remaining[j + p]
                            
                            idx_p_end = p_line.find("lbs")
                            if idx_p_end < 0:
                                num_pax += 1
                                errors.append("LEG " + leg_num + " PAX ERROR: Could not find 'lbs' in line '" + p_line + "'")
                                continue
                            idx_p_start = p_line.rfind("-", 0, idx_p_end)
                            if idx_p_start < 0:
                                idx_p_start = p_line.rfind("(", 0, idx_p_end)
                            if(idx_p_start < 0):
                                num_pax += 1
                                errors.append("LEG " + leg_num + " PAX ERROR: Could not find '-' or '(' in line '" + p_line + "'")
                                continue

                            passenger = int(p_line[idx_p_start + 1:idx_p_end].strip())
                            passengers.append(passenger)
                            baggage += 50
                        
                        j = j + num_pax
                    except: 
                        errors.append("LEG " + leg_num + " PAX ERROR: Could not find number of passengers in line '" + line + "'")                

                elif line.startswith("Totalpaxweight") or ("For" in line and "Name" in line and "Address" in line and "Phone" in line and "Conrmation" in line):
                    in_other = False
                    break 
                    
                if line.startswith("Other:") or in_other:   
                    in_other = True    
                    max_digits = 3
                    if " lbs" in line.lower():
                        idx_lbs = line.lower().find(" lbs") 
                        baggage += int(line[idx_lbs - max_digits:idx_lbs])
                        other.append(line)
            
            leg = Leg(leg_num, departure, destination, arrival_fuel_price, departure_fuel_price, fees, reserve, fuel_burn, passengers, baggage, distance, crew)
            legs.append(leg)       
    except Exception as e:
        errors.append(repr(e))
    trip = Trip(itinerary_num, 0, plane, legs, 0, 0, 0, 0, 0, errors)   
    return trip

if __name__ == "__main__":
    doc = read_doc(sys.argv[1])
    print(TripEncoder().encode(doc))

