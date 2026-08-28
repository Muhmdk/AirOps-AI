import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AirportApiService } from '../../core/services/airport-api.service';
import { AirportOperation, AirportRisk } from '../../core/models/airport.model';

@Component({ imports:[CommonModule,FormsModule,RouterLink], templateUrl:'./airports.page.html', styleUrl:'./airports.page.scss' })
export class AirportsPage {
  private readonly api=inject(AirportApiService); private readonly router=inject(Router);
  readonly dataSource=this.api.source;
  readonly airports=signal<AirportOperation[]>([]); readonly loading=signal(true); readonly search=signal(''); readonly risk=signal<'All'|AirportRisk>('All');
  readonly visible=computed(()=>{const q=this.search().toLowerCase().trim();return this.airports().filter(a=>(!q||`${a.code} ${a.name} ${a.city}`.toLowerCase().includes(q))&&(this.risk()==='All'||a.risk===this.risk())).sort((a,b)=>a.health-b.health)});
  readonly totals=computed(()=>({movements:this.airports().reduce((n,a)=>n+a.departures+a.arrivals,0),atRisk:this.airports().reduce((n,a)=>n+a.atRisk,0),delay:Math.round(this.airports().reduce((n,a)=>n+a.averageDelay,0)/(this.airports().length||1))}));
  constructor(){this.api.getAirports().subscribe(data=>{this.airports.set(data);this.loading.set(false)})}
  open(airport:AirportOperation){this.router.navigate(['/airports',airport.code])} clear(){this.search.set('');this.risk.set('All')}
}
