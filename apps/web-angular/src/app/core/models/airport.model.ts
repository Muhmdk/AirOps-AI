export type AirportRisk = 'Low' | 'Moderate' | 'High';
export interface AirportOperation {
  code: string; name: string; city: string; province: string; timezone: string;
  risk: AirportRisk; health: number; averageDelay: number; departures: number;
  arrivals: number; atRisk: number; gatesUsed: number; gatesTotal: number;
  weather: string; temperature: number; wind: string; visibility: string;
}
