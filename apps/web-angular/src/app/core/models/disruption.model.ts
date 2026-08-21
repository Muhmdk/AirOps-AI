export type DisruptionType =
  | 'Severe weather'
  | 'Aircraft maintenance'
  | 'Late incoming aircraft'
  | 'Gate conflict'
  | 'Airport congestion'
  | 'Crew timing issue'
  | 'Runway closure'
  | 'Air traffic restriction';
export type DisruptionSeverity = 'Moderate' | 'High' | 'Critical';
export interface ImpactedFlight {
  id: string;
  route: string;
  originalDelay: number;
  propagatedDelay: number;
  passengers: number;
  missedConnections: number;
  reason: string;
}
export interface PassengerConnectionImpact {
  inboundFlight: string;
  outboundFlight: string;
  connectionAirport: string;
  passengers: number;
  minimumConnectionMinutes: number;
  availableConnectionMinutes: number;
  status: 'Protected' | 'At risk' | 'Missed';
}
export interface GateConflictImpact {
  airport: string;
  gate: string;
  incomingFlight: string;
  occupyingFlight: string;
  overlapMinutes: number;
  severity: 'Warning' | 'Critical';
}
export interface CrewDutyImpact {
  crewId: string;
  flightId: string;
  role: string;
  projectedDutyMinutes: number;
  legalLimitMinutes: number;
  remainingMinutes: number;
  status: 'Monitor' | 'At risk' | 'Exceeded';
}
export interface NetworkImpact {
  affectedFlights: number;
  affectedPassengers: number;
  missedConnections: number;
  crewAffected: number;
  gateConflicts: number;
  hotelRooms: number;
  mealVouchers: number;
  estimatedCompensation: number;
  estimatedOperationalCost: number;
  recoveryMinutes: number;
  flights: ImpactedFlight[];
  connections: PassengerConnectionImpact[];
  gateDetails: GateConflictImpact[];
  crewDetails: CrewDutyImpact[];
}
export interface Disruption {
  id: string;
  type: DisruptionType;
  severity: DisruptionSeverity;
  status: 'Active' | 'Monitoring' | 'Resolved';
  airport: string;
  primaryFlight: string;
  startedAt: string;
  durationMinutes: number;
  description: string;
  impact: NetworkImpact;
  createdAt: string;
}
export interface DisruptionScenario {
  type: DisruptionType;
  severity: DisruptionSeverity;
  airport: string;
  flightId: string;
  durationMinutes: number;
}
export interface NetworkMutation {
  entityType: 'Flight' | 'Airport' | 'Aircraft';
  entityId: string;
  field: string;
  before: string | number;
  after: string | number;
}
export interface DisruptionAuditEntry {
  id: string;
  disruptionId: string;
  action: 'Created' | 'Resolved';
  actor: string;
  timestamp: string;
  summary: string;
  changes: NetworkMutation[];
}
export interface NetworkSnapshot {
  networkHealth: number;
  atRiskFlights: number;
  affectedPassengers: number;
  availableAircraft: number;
  estimatedCost: number;
}
export interface ScenarioRun {
  id: string;
  name: string;
  description: string;
  scenario: DisruptionScenario;
  before: NetworkSnapshot;
  after: NetworkSnapshot;
  disruptionId: string;
  runAt: string;
}
export interface CompoundDisruptionImpact {
  id: string;
  disruptionIds: string[];
  reason: string;
  sharedEntities: string[];
  additionalDelayMinutes: number;
  additionalPassengers: number;
  additionalCost: number;
  severity: 'High' | 'Critical';
}
