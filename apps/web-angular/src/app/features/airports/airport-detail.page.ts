import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AirportApiService } from '../../core/services/airport-api.service';
import { AirportOperation } from '../../core/models/airport.model';
import { SEEDED_FLIGHTS } from '../../core/services/flight-api.service';
@Component({imports:[RouterLink],templateUrl:'./airport-detail.page.html',styleUrl:'./airport-detail.page.scss'})
export class AirportDetailPage{
 private readonly api=inject(AirportApiService);private readonly route=inject(ActivatedRoute);readonly code=this.route.snapshot.paramMap.get('code')??'';readonly airport=signal<AirportOperation|null>(null);readonly flights=computed(()=>SEEDED_FLIGHTS.filter(f=>f.route.includes(this.code)));readonly loading=signal(true);
 constructor(){this.api.getAirports().subscribe(items=>{this.airport.set(items.find(a=>a.code===this.code)??null);this.loading.set(false)})}
}
