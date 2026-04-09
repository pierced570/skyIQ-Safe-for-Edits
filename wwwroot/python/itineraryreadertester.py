import itineraryreader
from pypdf import PdfReader #type ignore
import aiitineraryreader
import flighttestr28
import datetime
import sysconfig
import json
import math

def run_examples(start, end):
    f = open("E:\Projects\SkyIQ\SkyIQ\wwwroot\python\outputs.txt", "w")
    g = open("E:\Projects\SkyIQ\SkyIQ\wwwroot\python\errors.txt", "w")
 
    for i in range(start, end):
        filename = "E:\Projects\SkyIQ\examples\example" + str(i) + ".pdf"
        print(filename)
        doc = itineraryreader.read_doc(filename)        
        f.write(filename + "\n")
        f.write(itineraryreader.TripEncoder().encode(doc))
        f.write("\n\n")
        if len(doc.errors) > 0:
            g.write(filename + "\n")
            for error in doc.errors:
                print(error)
                g.write(error + "\n")        
            g.write("\n\n")

    f.close()
    g.close()

def extract_all_lines(filename):    
    reader = PdfReader(filename)
    allLines = ""
    for i in range(reader.get_num_pages()):        
        extracted = str(reader.pages[i].extract_text())
        allLines += extracted.encode("ascii", "ignore").decode()
    f = open("E:\Projects\SkyIQ\SkyIQ\wwwroot\python\extractedEx.txt", "w")
    f.write(allLines)
    f.close()
    #print(len(allLines))
    return allLines

def run_examples_ai(start, end, model, number):
    f = open("E:\Projects\SkyIQ\SkyIQ\wwwroot\python\openaitest_" + model + str(number) + ".txt", "w")
    for i in range(start, end):
        filename = "E:\Projects\SkyIQ\examples\example" + str(i) + ".pdf"
        f.write(filename + "\n")
        print(filename)
        text = extract_all_lines(filename)
        #print(text)
        start_time = datetime.datetime.now()
        parsed = aiitineraryreader.parse_text(text, model)
        end_time = datetime.datetime.now()
        total_time = end_time - start_time
        print(str(total_time))
        print(parsed)
        f.write(str(total_time) + "\n")
        f.write(parsed + "\n")
    f.close()

def run_example_ai(doc_num, model):
    filename = "E:\Projects\SkyIQ\examples\example" + str(doc_num) + ".pdf"
    
    print(filename)
    text = extract_all_lines(filename)
    parsed = aiitineraryreader.parse_text(text, model)
    trip = aiitineraryreader.convert_to_trip(parsed)
    print(trip)

    return trip

if __name__ == "__main__":
    model = "gpt-4o-mini"
    # run_examples_ai(1, 2, "gpt-4o", 0)
    # for i in range(0, 1):
    #     run_examples_ai(11, 22, model, i)

    # trip = run_example_ai(12, model)

    starting_fuel = 0
    #min_fuel_reserve_lbs = int(sys.argv[2])
    str = "{\"itinerary_num\":\"21SKYIQ\",\"starting_fuel\":0,\"aircraft\":\"NSKYIQ\",\"legs\":[{\"departure\":\"HHR\",\"destination\":\"SAN\",\"arrival_fuel_price\":null,\"departure_fuel_price\":[{\"min_fuel\":1.0,\"price\":6.18},{\"min_fuel\":200.0,\"price\":6.1},{\"min_fuel\":500.0,\"price\":5.65},{\"min_fuel\":750.0,\"price\":5.55},{\"min_fuel\":1000.0,\"price\":5.45}],\"fees\":[{\"name\":null,\"amount\":330.0,\"is_waivable\":true,\"waived_at\":150.0,\"airport\":\"SAN\"}],\"reserve\":1000.0,\"fuel_burn\":550.0,\"passengers\":[170.0,180.0,177.0,180.0,180.0,177.0],\"baggage\":200.0,\"leg_num\":\"1\",\"distance\":0.0,\"crew_weight\":[360.0],\"max_takeoff_weight\":13870.0,\"max_landing_weight\":12750.0,\"taxi_fuel_burn\":100.0,\"max_ramp_weight\":14000.0},{\"departure\":\"SAN\",\"destination\":\"APA\",\"arrival_fuel_price\":null,\"departure_fuel_price\":[{\"min_fuel\":1.0,\"price\":8.03},{\"min_fuel\":501.0,\"price\":7.97},{\"min_fuel\":1201.0,\"price\":7.38}],\"fees\":[{\"name\":null,\"amount\":820.0,\"is_waivable\":true,\"waived_at\":200.0,\"airport\":\"APA\"}],\"reserve\":1000.0,\"fuel_burn\":1450.0,\"passengers\":[170.0,180.0,177.0,180.0,180.0,177.0],\"baggage\":200.0,\"leg_num\":\"2\",\"distance\":0.0,\"crew_weight\":[360.0],\"max_takeoff_weight\":13870.0,\"max_landing_weight\":12750.0,\"taxi_fuel_burn\":100.0,\"max_ramp_weight\":14000.0},{\"departure\":\"APA\",\"destination\":\"ORL\",\"arrival_fuel_price\":null,\"departure_fuel_price\":[{\"min_fuel\":1.0,\"price\":3.56}],\"fees\":[{\"name\":null,\"amount\":275.0,\"is_waivable\":true,\"waived_at\":150.0,\"airport\":\"ORL\"}],\"reserve\":1000.0,\"fuel_burn\":2130.0,\"passengers\":[170.0,180.0,177.0,180.0,180.0,177.0],\"baggage\":200.0,\"leg_num\":\"3\",\"distance\":0.0,\"crew_weight\":[360.0],\"max_takeoff_weight\":13870.0,\"max_landing_weight\":12750.0,\"taxi_fuel_burn\":100.0,\"max_ramp_weight\":14000.0},{\"departure\":\"ORL\",\"destination\":\"DAL\",\"arrival_fuel_price\":null,\"departure_fuel_price\":[{\"min_fuel\":1.0,\"price\":5.13}],\"fees\":[{\"name\":null,\"amount\":260.0,\"is_waivable\":true,\"waived_at\":175.0,\"airport\":\"DAL\"}],\"reserve\":1000.0,\"fuel_burn\":2200.0,\"passengers\":[170.0,180.0,177.0,180.0,180.0,177.0],\"baggage\":200.0,\"leg_num\":\"4\",\"distance\":0.0,\"crew_weight\":[360.0],\"max_takeoff_weight\":13870.0,\"max_landing_weight\":12750.0,\"taxi_fuel_burn\":100.0,\"max_ramp_weight\":14000.0},{\"departure\":\"DAL\",\"destination\":\"HHR\",\"arrival_fuel_price\":null,\"departure_fuel_price\":[{\"min_fuel\":1.0,\"price\":3.58},{\"min_fuel\":501.0,\"price\":3.53},{\"min_fuel\":1201.0,\"price\":3.44}],\"fees\":[{\"name\":null,\"amount\":520.0,\"is_waivable\":true,\"waived_at\":200.0,\"airport\":\"HHR\"}],\"reserve\":1000.0,\"fuel_burn\":2345.0,\"passengers\":[177.0,177.0,177.0,177.0,177.0,177.0],\"baggage\":200.0,\"leg_num\":\"5\",\"distance\":0.0,\"crew_weight\":[360.0],\"max_takeoff_weight\":13870.0,\"max_landing_weight\":12750.0,\"taxi_fuel_burn\":100.0,\"max_ramp_weight\":14000.0},{\"departure\":\"HHR\",\"destination\":\"CRQ\",\"arrival_fuel_price\":null,\"departure_fuel_price\":[{\"min_fuel\":1.0,\"price\":6.21},{\"min_fuel\":200.0,\"price\":6.14},{\"min_fuel\":500.0,\"price\":5.69},{\"min_fuel\":750.0,\"price\":5.59},{\"min_fuel\":1000.0,\"price\":5.49}],\"fees\":[{\"name\":null,\"amount\":0.0,\"is_waivable\":true,\"waived_at\":0.0,\"airport\":null}],\"reserve\":1000.0,\"fuel_burn\":600.0,\"passengers\":[170.0,180.0,177.0,180.0,180.0,177.0],\"baggage\":200.0,\"leg_num\":\"6\",\"distance\":0.0,\"crew_weight\":[360.0],\"max_takeoff_weight\":13870.0,\"max_landing_weight\":12750.0,\"taxi_fuel_burn\":100.0,\"max_ramp_weight\":14000.0}],\"minimum_fuel_reserve\":0.0,\"max_fuel_reserve\":4710.0,\"max_takeoff_weight\":13870.0,\"max_landing_weight\":12750.0,\"basic_empty_weight\":8254.0,\"penalty\":0.072,\"lbs_per_hour\":700.0,\"errors\":[]}"
    trip = json.loads(str, cls = itineraryreader.TripDecoder)
    stops = flighttestr28.create_stops(trip)    
    optimal_cost = flighttestr28.find_optimal_strategy(stops, starting_fuel)    
    
    simple_cost = flighttestr28.get_simple_cost(stops, starting_fuel)
    flighttestr28.print_stop_details_json(stops, simple_cost)

#     trip = json.loads('''{"itinerary_num": "MHQYCS", "starting_fuel": 0, "aircraft": "N532LW", "legs": [{"departure": "CRQ", "destination": "SAN", "arrival_fuel_price": "", "departure_fuel_price": [{"min_fuel": 1, "price": 4.05}], "fees": [{"name": "Ground handling fee", "amount": 820.0, "is_waivable": true, "waived_at": 200.0}], "reserve": 1000, "fuel_burn": 600, "passengers": [], "baggage": 100, "leg_num": 1, "distance": 0, "crew_weight": [180, 180]}, {"departure": "SAN", "destination": "SFO", "arrival_fuel_price": "", "departure_fuel_price": [{"min_fuel": 1, "price": 8.11}], "fees": [{"name": "Ground handling fee", "amount": 570.0, "is_waivable": true, "waived_at": 200.0}], "reserve": 1000, "fuel_burn": 1500, "passengers": [180], "baggage": 150, "leg_num": 2, "distance": 0, "crew_weight": [180, 180]}, {"departure": "SFO", "destination": "SLC", "arrival_fuel_price": "", "departure_fuel_price": [{"min_fuel": 1, "price": 6.96}], "fees": [{"name": "Ground handling fee", "amount": 235.0, "is_waivable": true, "waived_at": 180.0}], "reserve": 1000, "fuel_burn": 1200, "passengers": [180], "baggage": 150, "leg_num": 3, "distance": 0, "crew_weight": [180, 180]}, {"departure": "SLC", "destination": "CRQ", "arrival_fuel_price": "", "departure_fuel_price": [{"min_fuel": 1, "price": 5.87}], "fees": [{"name": "Ground handling fee", "amount": 235.0, "is_waivable": true, "waived_at": 180.0}], "reserve": 1000, "fuel_burn": 2000, "passengers": [], "baggage": 100, "leg_num": 4, "distance": 0, "crew_weight": [180, 180]}], "minimum_fuel_reserve": 1000, "max_fuel_reserve": 
# 4710, "max_takeoff_weight": 13870, "max_landing_weight": 12750, "basic_empty_weight": 8254, "errors": []}''', cls = itineraryreader.TripDecoder)
#     stops = flighttestr28.create_stops(trip)    
#     optimal_cost = flighttestr28.find_optimal_strategy(stops, math.ceil(flighttestr28.lbs_to_gallons(int(1000))))    
#     flighttestr28.print_stop_details_json(stops)
    #print(sysconfig.get_paths()["purelib"]) 
