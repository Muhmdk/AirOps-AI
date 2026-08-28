export type PassengerJourneyStatus = 'Protected' | 'At risk' | 'Misconnected' | 'Rebooked';

export interface PassengerJourney {
  id: string;
  bookingReference: string;
  leadPassenger: string;
  partySize: number;
  loyaltyTier: string;
  currentFlightId: string;
  connectingFlightId: string;
  originCode: string;
  connectionAirport: string;
  destinationCode: string;
  minimumConnectionMinutes: number;
  availableConnectionMinutes: number;
  connectionShortfallMinutes: number;
  status: PassengerJourneyStatus;
  riskScore: number;
  specialServices: string[];
  alternativeFlights: string[];
  selectedAlternativeFlight: string | null;
  estimatedCareCost: number;
  rebookingNotes: string | null;
  updatedAt: string;
}
