export type AircraftStatus='Available'|'In service'|'Turnaround'|'Unavailable';
export interface AircraftOperation{registration:string;type:string;family:string;status:AircraftStatus;location:string;nextFlight:string;nextDeparture:string;utilization:number;cycles:number;hours:number;maintenanceDue:number;health:number;seats:number;range:string;}
