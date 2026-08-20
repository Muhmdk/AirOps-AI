export type FlightStatus = 'On time' | 'Delayed' | 'Boarding' | 'At risk';

export interface Flight {
  id: string;
  route: string;
  origin: string;
  destination: string;
  departure: string;
  arrival: string;
  aircraft: string;
  gate: string;
  status: FlightStatus;
  risk: number;
  passengers: number;
  connections: number;
  delay: number;
  riskLabel: string;
}

export interface OperationalEvent {
  time: string;
  type: 'risk' | 'delay' | 'gate' | 'ok';
  title: string;
  detail: string;
  accent: 'amber' | 'red' | 'blue' | 'green';
  severity?: 'Critical' | 'Warning' | 'Information';
  entityType?: 'flight' | 'airport' | 'aircraft';
  entityId?: string;
  category?: 'Weather' | 'Flight' | 'Gate' | 'Aircraft' | 'Passenger';
}
