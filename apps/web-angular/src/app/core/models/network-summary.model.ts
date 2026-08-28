export interface NetworkSummary {
  flightsToday: number;
  onTime: number;
  delayed: number;
  boarding: number;
  atRisk: number;
  cancelled: number;
  highRisk: number;
  passengers: number;
  connectingPassengers: number;
  networkHealth: number;
  airportsMonitored: number;
  airportAverageDelay: number;
  aircraftAvailable: number;
  aircraftUnavailable: number;
}
