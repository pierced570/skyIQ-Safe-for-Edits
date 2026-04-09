try:
    #from openai import OpenAI
    import requests
    from json import JSONDecoder
    import json
    import itineraryreader
    import sys
except Exception as e:
    print(repr(e))

api_key='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'

class Parsed_Data: 
    def __init__(self, crew_itinerary_id, aircraft_tail_number, trip_segments):
        self.crew_itinerary_id = crew_itinerary_id
        self.aircraft_tail_number = aircraft_tail_number
        self.trip_segments = trip_segments #list of trip segment 

class Trip_Segment:
    def __init__(self, leg, departure, destination, fuel_costs, passengers_weights, crew_weight, cargo_weight, fee_waivers):
            self.leg = leg
            self.departure = departure
            self.destination = destination
            self.fuel_costs = fuel_costs # list of fuel cost
            self.passengers_weights = passengers_weights # list of doubles
            self.crew_weight = crew_weight #list of doubles
            self.cargo_weight = cargo_weight #list of doubles                                                                     
            self.fee_waivers = fee_waivers # list of fee waiver    

class Fee_Waiver: 
     def __init__(self, fee_name, price, gallons_needed_to_waive, airport):
            self.fee_name = fee_name
            self.price = price
            self.gallons_needed_to_waive = gallons_needed_to_waive     
            self.airport = airport          

class DataDecoder(JSONDecoder):
    def __init__(self, *args, **kwargs):
        JSONDecoder.__init__(self, object_hook=self.object_hook, *args, **kwargs)
    def object_hook(self, obj):
        if("min_fuel" in obj):
            return itineraryreader.Fuel_Price(obj['min_fuel'], obj['price'])
        elif("gallons_needed_to_waive" in obj):
            return Fee_Waiver(obj['fee_name'], obj['price'], obj['gallons_needed_to_waive'], obj['airport'])
        elif("destination" in obj):
            return Trip_Segment(obj['leg'], obj['departure'], obj['destination'], obj['fuel_prices'], obj['passengers_weights'], [0], 0, obj['fee_waivers'])
        else:
            return Parsed_Data(obj["crew_itinerary_id"], obj['aircraft_tail_number'], obj['trip_segments'])
        

def parse_text(text, model):
    url = 'https://api.openai.com/v1/chat/completions'
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {api_key}"
    }
                        # Cargo Weight                       
                        # Provide a list of all baggage weights found. Include any weight listed as a number followed by lbs and that is not a person or total pax weight. List each weight only once. List all separately.
                        # Crew Weight
                        # For each crew member in the leg, if their weight is specified, use that value.
                        # If not, use 180 lbs.
                        # Provide a list of all crew weights per leg.
                                # "crew_weight": [float],
                                # "cargo_weight": [float],        
    payload = {
        "model": model,
        "messages": [{
            "role": "user",
            "content": """
                        I need you to output a JSON object that contains data from the attached PDF document.  Please extract structured data from the string related to trip details, ensuring accuracy and completeness. Follow these specific guidelines for each data point: 
                        Crew Itinerary Id
                        Aircraft Tail Number
                        Extract and provide the Tail Number. Tail number is a 2-6 digit code and does not include a manufacturer name.
                        Trip Segments (Per Leg of the Trip)
                        Departure & Destination: Extract airport codes. Do not provide times for departure or arrival.
                        Fuel Cost: Provide a list of all the cost per gallon at each airport. In each fuel cost, include the price of the fuel and the minimum amount of fuel needed to use that price. If only one fuel price is listed, the minimum amount of fuel should be set to 1.
                        Passengers & Weight
                        If a passenger weight is specified, use that value.
                        If not specified, assume -1 lbs per passenger.
                        Provide a comma-delimited list of all passenger weights. 
                        Fee Waivers
                        Identify all fees that include "waived with" or "ww" or "ww/". List the price of the fee, the number of gallons needed to waive it, and the port associated with it. If no port is listed, set port to the destination.
                        Return only fees that are waivable. If no fees are waivable, return an empty list.

                        Output Format:
                        {
                            "crew_itinerary_id": string,
                            "aircraft_tail_number": string,
                            "trip_segments": [
                                {
                                "leg": number,
                                "departure": string,
                                "destination": string,
                                "fuel_prices": [
                                    {
                                    "price": float,
                                    "min_fuel": float
                                    }
                                ],
                                "passengers_weights": [float],                                                             
                                "fee_waivers": [
                                    {       
                                    "fee_name": string,                                 
                                    "price": float,
                                    "gallons_needed_to_waive": float, 
                                    "airport": string
                                    }
                                ]
                            }]
                        }
                            Do not include any summaries or comments in the output. Do not include the word "json". All comments that start with '//' must be removed
                            the string is ###"""+ text + """###"""
        }]
    }
    response = requests.post(url, headers=headers, json=payload)
    if response.status_code == 200:
        return response.json()["choices"][0]["message"]["content"].strip()
    else: 
        return None
    # completion = client.chat.completions.create(
    #     model=model, 
    #     messages=[{
    #         "role":"user",
    #         "content":
    #     }]
    # )

    #return completion.choices[0].message.content


def convert_to_trip(data):
    decoded = json.loads(data, cls = DataDecoder)
    itinerary_num = decoded.crew_itinerary_id
    aircraft = decoded.aircraft_tail_number
    starting_fuel = minimum_fuel_reserve = max_fuel_reserve = max_takeoff = max_landing = basic_empty_weight = 0
    errors = []
    legs = []

    for segment in decoded.trip_segments:
        leg_num = segment.leg 
        departure = segment.departure
        destination = segment.destination
        arrival_fuel_price = ""
        departure_fuel_price = segment.fuel_costs
        fees = []
        reserve = fuel_burn = distance = max_takeoff_weight = max_landing_weight = max_ramp_weight = taxi_fuel_burn = penalty = lbs_per_hour= 0
        passengers = segment.passengers_weights
        crew_weight = [0]
        baggage = 0
        for waiver in segment.fee_waivers:
            if(float(waiver.price) > 0 and float(waiver.gallons_needed_to_waive) > 0):
                fees.append(itineraryreader.Fee(waiver.fee_name, float(waiver.price), True, float(waiver.gallons_needed_to_waive), waiver.airport))
        
        legs.append(itineraryreader.Leg(leg_num, departure, destination, arrival_fuel_price, departure_fuel_price, fees, reserve, fuel_burn, passengers, baggage, distance, crew_weight, max_takeoff_weight, max_landing_weight, max_ramp_weight, taxi_fuel_burn))

    return itineraryreader.Trip(itinerary_num, starting_fuel, aircraft, legs, minimum_fuel_reserve, max_fuel_reserve, max_takeoff, max_landing, basic_empty_weight, errors, penalty, lbs_per_hour)

if __name__ == "__main__":
    try:
        data = itineraryreader.extract_all_lines(sys.argv[1])#"E:\Projects\SkyIQ\examples\example30.pdf")
        data = parse_text(data, sys.argv[2])#"gpt-4o-mini")
        if data is not None:
            trip = convert_to_trip(data)
            print(itineraryreader.TripEncoder().encode(trip))
    except Exception as e:
        print("Error: " + repr(e))

