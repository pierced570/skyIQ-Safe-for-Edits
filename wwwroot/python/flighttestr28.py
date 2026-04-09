import math
import json
import sys
import itineraryreader

GALS_TO_LBS = 6.7  # Conversion factor from gallons to pounds
LBS_TO_GALS = 1 / GALS_TO_LBS  # Conversion factor from pounds to gallons

class Stop:
    def __init__(self, name, tiers, fuel_burn, leg_fees, tank_capacity_gallons_takeoff, tank_capacity_gallons_landing, arrival, departure, fixed_weights, min_fuel_reserve, max_ramp_weight, taxi_fuel_burn, penalty, gals_per_hour):
        """
        We treat each tuple in 'tiers' as (min_quantity_in_gallons, price_per_gallon).
        This means if you buy at least 'min_quantity_in_gallons', the rate applies to all gallons.
        The code should assume tiers is sorted in ascending order by min_quantity.
        e.g. [ (0, $5.50), (300, $5.25), (600, $5.10) ]
        So if you buy 250 gallons, you pay $5.50 * 250.  If you buy 400 gallons, you pay $5.25 * 400.
        If you buy 800 gallons, you pay $5.10 * 800, etc.
        """
        self.name = name
        self.tiers = tiers  # e.g. [(0, 5.0), (300, 4.5), (500, 4.0)] etc.
        self.fuel_burn = fuel_burn  # Fuel burn to the next stop in lbs
        self.total_fuel_purchased = 0
        self.total_cost = 0
        self.avg_cost_per_gallon = 0
        self.fees = leg_fees
        self.tank_capacity_gallons_landing = tank_capacity_gallons_landing
        self.tank_capacity_gallons_takeoff = tank_capacity_gallons_takeoff
        self.starting_fuel = 0
        self.start_fuel = 0
        self.takeoff_fuel = 0
        self.landing_fuel = 0
        self.arrival = arrival
        self.departure = departure
        self.fixed_weights = fixed_weights
        self.min_fuel_reserve = min_fuel_reserve
        self.max_ramp_weight = max_ramp_weight
        self.taxi_fuel_burn = taxi_fuel_burn        
        self.penalty = penalty
        self.gals_per_hour = gals_per_hour
        self.errors = []

    def cost_of_purchase(self, buy, burn, landing, min_fuel_reserve_gallons):
        """
        Return the total cost to purchase x gallons at this stop.
        We interpret each tier as a minimum quantity.
        If x >= that tier's min, that tier's price applies to all x gallons.
        We'll pick the highest tier whose min_quantity <= x.
        """
        fee_total = 0.0
        if(len(self.fees) > 0):
            fee = self.fees[0] #should be only one fuel fee
            if(buy < int(fee.waived_at)):
                fee_total = fee.amount

        if buy <= 0:            
            return fee_total
        # We'll track the best (lowest) price for which x >= min_q
        applicable_price = None
        for (min_q, price) in self.tiers:
            if buy >= min_q:
                applicable_price = price
            else:
                # since tiers are sorted ascending by min_q, once x < min_q, we can break.
                break
        if applicable_price is None:
            # in case the first tier has a min_q > x, fallback to that tier's price (or 0)
            # But typically you'd expect the first tier to start at 0.
            applicable_price = self.tiers[0][1]
        
        fuel_penalty = 0
        extra_fuel = landing - min_fuel_reserve_gallons
        if(extra_fuel > 0):
            hours = burn / (self.gals_per_hour if self.gals_per_hour > 0 else 1)
            fuel_penalty = extra_fuel * hours * self.penalty

        return (buy * applicable_price) + fee_total + (fuel_penalty * applicable_price)

def get_simple_cost(stops, start_fuel):
    fuel_burns = [int(math.ceil(lbs_to_gallons(stop.fuel_burn))) for stop in stops]
    total_cost = 0
    for i in range(len(stops)):
        stop = stops[i]
        min_fuel_reserve_gallons = int(math.ceil(lbs_to_gallons(stop.min_fuel_reserve)))   
        burn = fuel_burns[i]  
        if(i == 0):
            burn = burn + stop.min_fuel_reserve - start_fuel if stop.min_fuel_reserve > start_fuel else burn
        total_cost = total_cost + stop.cost_of_purchase(burn + stop.taxi_fuel_burn, burn, stop.min_fuel_reserve, min_fuel_reserve_gallons)
    return total_cost


def lbs_to_gallons(weight_in_lbs):
    return weight_in_lbs * LBS_TO_GALS

def gallons_to_lbs(gallons):
    return gallons * GALS_TO_LBS

def find_optimal_strategy(stops, start_fuel):
    """
    Dynamic Programming approach to determine a cost-minimized refueling strategy:
    1. Never let fuel drop below the reserve.
    2. Ensure you have enough fuel to travel to the next stop.
    3. Attempt to minimize cost over the entire route.

    We'll assume each stop's 'cost_of_purchase(x)' picks the single price whose min_threshold <= x.
    """
    n = len(stops)
    #tank_capacity_gallons = int(math.floor(lbs_to_gallons(tank_capacity_lbs)))
    #min_fuel_reserve_gallons = int(math.ceil(lbs_to_gallons(min_fuel_reserve_lbs))) 

    # Convert each stop's fuel burn from lbs to integer gallons (round up to ensure no shortfall)
    fuel_burns = [int(math.ceil(lbs_to_gallons(stop.fuel_burn))) for stop in stops]

    # DP[i][f] = minimal cost to arrive at stop i with f gallons in the tank.
    # i from 0..n, where i=0 means 'before the first stop', i=n means 'after the last stop'.
    # We'll store them in a list of dicts to handle only feasible fuel states.

    DP = [dict() for _ in range(n+1)]
    parent = [dict() for _ in range(n+1)]  # to reconstruct solution (store (prev_fuel, gallons_bought))
    # Start at stop 0 with a full tank.
    DP[0][start_fuel] = 0.0
    parent[0][start_fuel] = (-1, 0)
    has_critical_errors = False
    for i in range(n):
        stop = stops[i]
        burn = fuel_burns[i]   
        min_fuel_reserve_gallons = int(math.ceil(lbs_to_gallons(stop.min_fuel_reserve)))   
        if(stop.tank_capacity_gallons_landing < 0):
            stop.errors.append("ERROR: Tank landing capacity is under 0: " + str(stop.tank_capacity_gallons_landing))        
        if(stop.tank_capacity_gallons_takeoff < 0):
            stop.errors.append("ERROR: Tank takeoff capacity is under 0: " + str(stop.tank_capacity_gallons_takeoff))
        if(stop.max_ramp_weight < 0):
            stop.errors.append("ERROR: Too heavy for ramp")   
        if(stop.max_ramp_weight < stop.tank_capacity_gallons_takeoff):
            stop.errors.append("ERROR: Max ramp weight is less than calculated fuel capacity")     

        if(len(stop.errors) > 0):            
            has_critical_errors = True
            continue
        x = stop.tank_capacity_gallons_takeoff + stop.taxi_fuel_burn
        for f_current, cost_so_far in list(DP[i].items()):
            max_buy = stop.max_ramp_weight - f_current
            if(max_buy <= 0): # too heavy for ramp
                continue    
            if (stop.tank_capacity_gallons_takeoff + stop.taxi_fuel_burn) < max_buy:
                max_buy =  stop.tank_capacity_gallons_takeoff + stop.taxi_fuel_burn
            # max_buy = stop.tank_capacity_gallons_takeoff - f_current
            # max_buy = max_buy if max_buy >= 0 else stop.tank_capacity_gallons_takeoff
            
            for buy in range(max_buy+1):
                # Get taxi weight, check if weight after taxi is too much for liftoff
                new_fuel = f_current + buy - stop.taxi_fuel_burn 
                if new_fuel > stop.tank_capacity_gallons_takeoff: 
                    continue
                # Get fly weight, check 
                new_fuel = new_fuel - burn
                if new_fuel < min_fuel_reserve_gallons:
                    continue
                if new_fuel < 0:
                    continue
                if new_fuel > stop.tank_capacity_gallons_landing:
                    continue
                purchase_cost = stop.cost_of_purchase(buy, burn, new_fuel, min_fuel_reserve_gallons)
                new_cost = cost_so_far + purchase_cost
                if new_fuel not in DP[i+1] or DP[i+1][new_fuel] > new_cost:
                    DP[i+1][new_fuel] = new_cost
                    parent[i+1][new_fuel] = (f_current, buy)
    
    # Find minimal cost among states at stop n.
    if(has_critical_errors):
        return 0
    best_cost = float('inf')
    best_fuel = None
    for f, cost_val in DP[n].items():
        if f >= 0 and cost_val < best_cost:
            best_cost = cost_val
            best_fuel = f
    
    # Reconstruct how many gallons were purchased at each stop
    reconstruction = [0] * n
    cur_i = n
    cur_f = best_fuel 
    while cur_i > 0:
        if(cur_f is None): 
            stops[cur_i-1].errors.append("ERROR: No fuel options found and calculations quit")
            break
        prev_f, bought = parent[cur_i][cur_f]
        reconstruction[cur_i - 1] = bought
        cur_f = prev_f
        cur_i -= 1
    
    # Apply to stops and track actual usage
    current_fuel = start_fuel
    total_cost = 0.0

    for i, stop in enumerate(stops):
        burn_gallons = fuel_burns[i]
        burn_lbs = stop.fuel_burn  # original burn in lbs
        buy = reconstruction[i]

        # Fuel before
        fuel_before = current_fuel
        fuel_before_lbs = gallons_to_lbs(fuel_before)

        # Fuel after purchase
        fuel_after_purchase = current_fuel + buy
        fuel_after_purchase_lbs = gallons_to_lbs(fuel_after_purchase)
        stop.start_fuel = fuel_after_purchase_lbs 
        stop.takeoff_fuel = fuel_after_purchase_lbs - gallons_to_lbs(stop.taxi_fuel_burn)
        
        if stop.takeoff_fuel < min_fuel_reserve_gallons and i != n-1:
            #raise ValueError("DP solution invalid - went below reserve.")
            stop.errors.append("ERROR: Takeoff fuel of " + str(math.floor(gallons_to_lbs(current_fuel))) + " is below the reserve set at " + str(stop.min_fuel_reserve))    

        # Burn fuel going to next stop
        current_fuel = fuel_after_purchase - stop.taxi_fuel_burn - burn_gallons
        fuel_remaining_lbs = gallons_to_lbs(current_fuel)
        stop.landing_fuel = fuel_remaining_lbs

        cost = stop.cost_of_purchase(buy, burn_gallons, fuel_remaining_lbs, min_fuel_reserve_gallons)

        # Purchase
        stop.total_fuel_purchased = buy
        stop.total_cost = cost
        # The cost is buy * the price for which x >= min_q
        price_per_gal = cost / buy if buy > 0 else 0.0
        stop.avg_cost_per_gallon = price_per_gal
        total_cost += cost

        # Print tier pricing info:
        tier_info = "".join(
            [f"    Tier {idx+1}: min {t[0]} gal => ${t[1]:.2f}/gal\n" for idx, t in enumerate(stop.tiers)]
        )

        if current_fuel < min_fuel_reserve_gallons and i != n-1:
            #raise ValueError("DP solution invalid - went below reserve.")
            stop.errors.append("ERROR: Landing fuel of " + str(gallons_to_lbs(current_fuel)) + " is below the reserve set at " + str(stop.min_fuel_reserve))            

    return best_cost

def create_stops(data):
    """Dynamically generate a list of stops from itineraryreader."""
    stop_data = []
    trip_test = itineraryreader.read_doc(str(1))if data is None else data
    
    for i, leg in enumerate(trip_test.legs):
        fuel_burn_mapping = {0: 400, 1: 2750, 2: 600, 3: 1000, 4: 2100, 5: 1100, 6: 2600, 7: 1100, 8: 2200}
        if(data is None):
            leg.fuel_burn = fuel_burn_mapping.get(i, 800)

        # We'll interpret each FuelPrice as: (min_fuel_gallons, price)
        # e.g., if fp.min_fuel=300 => if you buy >=300, price=fp.price.
        # So we'll just collect them and sort ascending by min_fuel.
        fuel_tiers = sorted([(fp.min_fuel, fp.price) for fp in leg.departure_fuel_price], key=lambda x: x[0])
        
        leg_fees = [itineraryreader.Fee(fp.name, fp.amount, fp.is_waivable, fp.waived_at, fp.airport) for fp in leg.fees]
        fixed_weights = trip_test.basic_empty_weight + leg.baggage + sum(leg.crew_weight) + sum(leg.passengers)   
        leg_tank_cap_lbs = leg.max_takeoff_weight - fixed_weights
        leg_tank_land_lbs = leg.max_landing_weight - fixed_weights
        leg_tank_cap_lbs = leg_tank_cap_lbs if leg_tank_cap_lbs < trip_test.max_fuel_reserve else trip_test.max_fuel_reserve
        leg_tank_land_lbs = leg_tank_land_lbs if leg_tank_land_lbs < trip_test.max_fuel_reserve else trip_test.max_fuel_reserve
        leg_max_ramp = leg.max_ramp_weight - fixed_weights
        leg_max_ramp = leg_max_ramp if leg_max_ramp < trip_test.max_fuel_reserve else trip_test.max_fuel_reserve
        
        stop_data.append(("Stop " + str(i + 1), fuel_tiers, leg.fuel_burn, leg_fees, math.floor(lbs_to_gallons(leg_tank_cap_lbs)), 
                          math.floor(lbs_to_gallons(leg_tank_land_lbs)), leg.destination, leg.departure, fixed_weights, leg.reserve, 
                          math.floor(lbs_to_gallons(leg_max_ramp)), math.ceil(lbs_to_gallons(leg.taxi_fuel_burn)), trip_test.penalty, round(lbs_to_gallons(trip_test.lbs_per_hour), 2)))
        

    return [Stop(name, tiers, fuel_burn, fees, tank_cap_gals, tank_cap_gals_land, arrival, departure, fixed_weights, reserve, max_ramp_weight, taxi_fuel_burn, penalty, gals_per_hour) 
            for name, tiers, fuel_burn, fees, tank_cap_gals, tank_cap_gals_land, arrival, departure, fixed_weights, reserve, max_ramp_weight, taxi_fuel_burn, penalty, gals_per_hour in stop_data]

####################################
# Additional JSON Output Routines
####################################
def stops_to_json(stops):
    """Return a list of dictionaries summarizing each stop."""
    data = []
    for stop in stops:
        data.append({
            "name": stop.name,
            "arrival": stop.arrival,
            "departure": stop.departure,
            "fees": [
                {
                    "amount": fee.amount,
                    "is_waivable": fee.is_waivable, 
                    "waived_at": fee.waived_at
                } for fee in stop.fees
            ],
            "fuel_burn_lbs": stop.fuel_burn, 
            "start_fuel": stop.start_fuel,
            "takeoff_fuel_lbs": stop.takeoff_fuel,
            "landing_fuel_lbs": stop.landing_fuel,
            "total_fuel_purchased_gallons": stop.total_fuel_purchased,
            "total_fuel_purchased_lbs": gallons_to_lbs(stop.total_fuel_purchased),
            "total_cost": stop.total_cost,
            "avg_cost_per_gallon": stop.avg_cost_per_gallon,
            "fixed_weights": stop.fixed_weights,
            "errors": stop.errors
        })
    return data

def print_stop_details_json(stops, simple_cost):
    """Prints a JSON representation of all stop details."""
    data = stops_to_json(stops)
    print("{simple_cost: " + str(simple_cost) + ", data:" + json.dumps(data, indent=2) + "}")


def print_final_totals_json(optimal_cost, stops):
    """Prints a JSON representation of the final trip totals."""
    total_fuel = sum(s.total_fuel_purchased for s in stops)
    data = {
        "total_cost": optimal_cost,
        "total_fuel_purchased": total_fuel
    }
    print(json.dumps(data, indent=2))

# Example Usage
if __name__ == "__main__":
    starting_fuel = math.floor(lbs_to_gallons(int(sys.argv[1])))
    #min_fuel_reserve_lbs = int(sys.argv[2])
    trip = json.loads(sys.argv[3], cls = itineraryreader.TripDecoder)
    stops = create_stops(trip)    
    optimal_cost = find_optimal_strategy(stops, starting_fuel)    
    simple_cost = get_simple_cost(stops, starting_fuel)
    print_stop_details_json(stops, simple_cost)
    
