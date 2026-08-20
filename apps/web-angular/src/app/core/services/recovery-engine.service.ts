import { Injectable, computed, signal } from '@angular/core';
import { Disruption } from '../models/disruption.model';
import {
  OperationalRisk,
  RecoveryActionType,
  RecoveryAuditEntry,
  RecoveryPlan,
} from '../models/recovery.model';
import { DisruptionEngineService } from './disruption-engine.service';
import { AircraftApiService } from './aircraft-api.service';
import { FlightApiService } from './flight-api.service';
import { AirportApiService } from './airport-api.service';
import { OperationsEventService } from './operations-event.service';
interface Candidate {
  action: RecoveryActionType;
  name: string;
  description: string;
  delayFactor: number;
  costFactor: number;
  connectionFactor: number;
  recoveryFactor: number;
  risk: OperationalRisk;
  advantages: string[];
  disadvantages: string[];
}
const CANDIDATES: Candidate[] = [
  {
    action: 'Maintain rotation',
    name: 'Maintain current rotation',
    description: 'Keep the assigned aircraft and absorb the propagated delay.',
    delayFactor: 1,
    costFactor: 1,
    connectionFactor: 1,
    recoveryFactor: 1,
    risk: 'Medium',
    advantages: ['No aircraft or crew reassignment', 'Lowest implementation complexity'],
    disadvantages: ['Longest passenger delay', 'Delay continues through downstream rotation'],
  },
  {
    action: 'Swap aircraft',
    name: 'Swap with available aircraft',
    description: 'Assign a compatible available aircraft and protect the original rotation.',
    delayFactor: 0.34,
    costFactor: 0.62,
    connectionFactor: 0.24,
    recoveryFactor: 0.46,
    risk: 'Low',
    advantages: ['Reduces downstream delay', 'Protects most passenger connections'],
    disadvantages: [
      'Requires compatible spare aircraft',
      'Creates an aircraft repositioning requirement',
    ],
  },
  {
    action: 'Hold connecting flight',
    name: 'Protect passenger connections',
    description: 'Hold the highest-value outbound connection while the disrupted flight arrives.',
    delayFactor: 0.72,
    costFactor: 0.78,
    connectionFactor: 0.38,
    recoveryFactor: 0.82,
    risk: 'Medium',
    advantages: ['Protects connecting passengers', 'Avoids large-scale rebooking'],
    disadvantages: ['Transfers delay to another flight', 'May create another gate conflict'],
  },
  {
    action: 'Change gate',
    name: 'Reassign to compatible gate',
    description: 'Move the primary flight to a nearby compatible gate and remove the occupancy conflict.',
    delayFactor: 0.48,
    costFactor: 0.56,
    connectionFactor: 0.62,
    recoveryFactor: 0.52,
    risk: 'Low',
    advantages: ['Clears the active gate conflict', 'Requires no aircraft reassignment'],
    disadvantages: ['Requires passenger and ramp-team movement', 'Gate compatibility must be reconfirmed'],
  },
  {
    action: 'Cancel downstream flight',
    name: 'Cancel lowest-impact downstream leg',
    description: 'Break the affected rotation by cancelling its lowest-demand downstream service.',
    delayFactor: 0.22,
    costFactor: 1.18,
    connectionFactor: 0.55,
    recoveryFactor: 0.38,
    risk: 'High',
    advantages: ['Stops rotation delay propagation', 'Restores aircraft schedule quickly'],
    disadvantages: ['Requires passenger reaccommodation', 'Highest customer-service impact'],
  },
  {
    action: 'Rebook passengers',
    name: 'Proactive passenger rebooking',
    description: 'Keep the operation unchanged while moving at-risk connections to alternatives.',
    delayFactor: 0.9,
    costFactor: 0.86,
    connectionFactor: 0.16,
    recoveryFactor: 0.92,
    risk: 'Low',
    advantages: ['Minimizes missed connections', 'Can begin before flight arrival'],
    disadvantages: ['Does not improve aircraft rotation', 'Consumes available seat inventory'],
  },
];
@Injectable({ providedIn: 'root' })
export class RecoveryEngineService {
  readonly plans = signal<Record<string, RecoveryPlan[]>>({});
  readonly auditEntries = signal<RecoveryAuditEntry[]>([]);
  readonly generatedCount = computed(() => Object.keys(this.plans()).length);
  constructor(
    private readonly disruptions: DisruptionEngineService,
    private readonly aircraft: AircraftApiService,
    private readonly flights?: FlightApiService,
    private readonly airports?: AirportApiService,
    private readonly events?: OperationsEventService,
  ) {
    this.plans.set(this.read<Record<string, RecoveryPlan[]>>('airops-recovery-plans') ?? {});
    this.auditEntries.set(this.read<RecoveryAuditEntry[]>('airops-recovery-audit') ?? []);
    this.restoreApprovedOutcomes();
  }
  forDisruption(id: string) {
    const disruption = this.disruptions.get(id);
    if (!disruption) return [];
    const existing = this.plans()[id];
    const belongsToCurrentDisruption =
      existing?.[0] &&
      new Date(existing[0].createdAt).getTime() >= new Date(disruption.createdAt).getTime();
    const usesCurrentPlanSchema =
      existing?.some((plan) => plan.action === 'Change gate') ||
      existing?.some((plan) => plan.status !== 'Proposed');
    if (existing && belongsToCurrentDisruption && usesCurrentPlanSchema) return existing;
    return this.generate(disruption);
  }
  generate(disruption: Disruption) {
    const maxDelay = Math.max(...disruption.impact.flights.map((f) => f.propagatedDelay));
    const available = this.aircraft.state().filter((a) => a.status === 'Available');
    const plans: RecoveryPlan[] = CANDIDATES.filter(
      (c) => c.action !== 'Swap aircraft' || available.length > 0,
    )
      .map((candidate, index) => {
        const delay = Math.max(8, Math.round(maxDelay * candidate.delayFactor));
        const cost = Math.round(
          disruption.impact.estimatedOperationalCost * candidate.costFactor +
            (candidate.action === 'Swap aircraft' ? 6500 : 0),
        );
        const missed = Math.round(disruption.impact.missedConnections * candidate.connectionFactor);
        const recovery = Math.round(disruption.impact.recoveryMinutes * candidate.recoveryFactor);
        const breakdown = {
          delay: this.inverseScore(delay, 140),
          cost: this.inverseScore(cost, 160000),
          passengers: this.inverseScore(missed, 80),
          risk: candidate.risk === 'Low' ? 95 : candidate.risk === 'Medium' ? 70 : 38,
        };
        const score = Math.round(
          breakdown.delay * 0.3 +
            breakdown.cost * 0.25 +
            breakdown.passengers * 0.3 +
            breakdown.risk * 0.15,
        );
        return {
          id: `RCP-${disruption.id.split('-')[1]}-${index + 1}`,
          disruptionId: disruption.id,
          name: candidate.name,
          action: candidate.action,
          description: candidate.description,
          flightsAffected: disruption.impact.flights.map((f) => f.id),
          aircraftAffected: candidate.action === 'Swap aircraft' ? [available[0].registration] : [],
          passengersAffected: disruption.impact.affectedPassengers,
          missedConnections: missed,
          expectedDelayMinutes: delay,
          recoveryMinutes: recovery,
          estimatedCost: cost,
          operationalRisk: candidate.risk,
          advantages: candidate.advantages,
          disadvantages: candidate.disadvantages,
          score,
          recommended: false,
          scoreBreakdown: breakdown,
          status: 'Proposed' as const,
          createdAt: new Date().toISOString(),
        };
      })
      .sort((a, b) => b.score - a.score);
    if (plans[0]) plans[0] = { ...plans[0], recommended: true };
    this.plans.update((state) => ({ ...state, [disruption.id]: plans }));
    this.persist();
    return plans;
  }
  requiresSupervisor(plan: RecoveryPlan) {
    return plan.operationalRisk === 'High' || plan.estimatedCost >= 75000;
  }
  approve(planId: string, notes: string, supervisorOverride = false) {
    const plan = this.findPlan(planId);
    if (!plan) return { ok: false, error: 'Recovery plan not found' };
    if (plan.status !== 'Proposed')
      return { ok: false, error: 'Only proposed recovery plans can be approved' };
    if (this.requiresSupervisor(plan) && !supervisorOverride)
      return { ok: false, error: 'Supervisor approval is required for this plan' };
    const disruption = this.disruptions.get(plan.disruptionId);
    if (!disruption) return { ok: false, error: 'Disruption not found' };
    this.plans.update((state) => ({
      ...state,
      [plan.disruptionId]: state[plan.disruptionId].map((item) => ({
        ...item,
        status: item.id === plan.id ? ('Approved' as const) : ('Rejected' as const),
      })),
    }));
    this.disruptions.resolve(plan.disruptionId);
    const approved = this.findPlan(plan.id)!;
    this.flights?.applyRecovery(approved);
    this.aircraft.applyRecovery(approved);
    this.airports?.applyRecovery(disruption.airport, approved);
    this.auditEntries.update((entries) => [
      {
        id: `RCA-${Date.now()}`,
        planId: plan.id,
        disruptionId: plan.disruptionId,
        action: 'Approved',
        actor: supervisorOverride ? 'Alex Morgan' : 'Maya Chen',
        actorRole: supervisorOverride ? 'Operations Supervisor' : 'Operations Controller',
        timestamp: new Date().toISOString(),
        notes,
        supervisorOverride,
        outcome: {
          delayBefore: disruption.impact.flights[0].propagatedDelay,
          delayAfter: plan.expectedDelayMinutes,
          costBefore: disruption.impact.estimatedOperationalCost,
          costAfter: plan.estimatedCost,
          missedBefore: disruption.impact.missedConnections,
          missedAfter: plan.missedConnections,
        },
      },
      ...entries,
    ]);
    this.events?.publish({
      time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      type: 'ok',
      title: `Recovery approved · ${plan.name}`,
      detail: `${plan.disruptionId} · ${plan.expectedDelayMinutes} min expected delay`,
      accent: 'green',
      severity: 'Information',
      entityType: 'flight',
      entityId: disruption.primaryFlight,
      category: 'Flight',
    });
    this.persist();
    return { ok: true, error: '' };
  }
  reject(planId: string, notes: string) {
    const plan = this.findPlan(planId);
    if (!plan) return false;
    const disruption = this.disruptions.get(plan.disruptionId);
    this.plans.update((state) => ({
      ...state,
      [plan.disruptionId]: this.promoteBestProposed(
        state[plan.disruptionId].map((item) =>
          item.id === planId
            ? { ...item, status: 'Rejected' as const, recommended: false }
            : item,
        ),
      ),
    }));
    this.auditEntries.update((entries) => [
      {
        id: `RCA-${Date.now()}`,
        planId,
        disruptionId: plan.disruptionId,
        action: 'Rejected',
        actor: 'Maya Chen',
        actorRole: 'Operations Controller',
        timestamp: new Date().toISOString(),
        notes,
        supervisorOverride: false,
        outcome: {
          delayBefore: disruption?.impact.flights[0]?.propagatedDelay ?? 0,
          delayAfter: disruption?.impact.flights[0]?.propagatedDelay ?? 0,
          costBefore: disruption?.impact.estimatedOperationalCost ?? 0,
          costAfter: disruption?.impact.estimatedOperationalCost ?? 0,
          missedBefore: disruption?.impact.missedConnections ?? 0,
          missedAfter: disruption?.impact.missedConnections ?? 0,
        },
      },
      ...entries,
    ]);
    if (disruption)
      this.events?.publish({
        time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
        type: 'gate',
        title: `Recovery rejected · ${plan.name}`,
        detail: `${plan.disruptionId} remains active for further recovery review`,
        accent: 'blue',
        severity: 'Information',
        entityType: 'flight',
        entityId: disruption.primaryFlight,
        category: 'Flight',
      });
    this.persist();
    return true;
  }
  private promoteBestProposed(plans: RecoveryPlan[]) {
    const next = plans.find((plan) => plan.status === 'Proposed');
    return plans.map((plan) => ({ ...plan, recommended: plan.id === next?.id }));
  }
  private findPlan(id: string) {
    return Object.values(this.plans())
      .flat()
      .find((plan) => plan.id === id);
  }
  private restoreApprovedOutcomes() {
    for (const plan of Object.values(this.plans())
      .flat()
      .filter((item) => item.status === 'Approved')) {
      const disruption = this.disruptions.get(plan.disruptionId);
      this.flights?.applyRecovery(plan);
      this.aircraft.applyRecovery(plan);
      if (disruption) this.airports?.applyRecovery(disruption.airport, plan);
    }
  }
  private persist() {
    if (typeof localStorage === 'undefined') return;
    localStorage.setItem('airops-recovery-plans', JSON.stringify(this.plans()));
    localStorage.setItem('airops-recovery-audit', JSON.stringify(this.auditEntries()));
  }
  private read<T>(key: string): T | null {
    if (typeof localStorage === 'undefined') return null;
    try {
      const value = localStorage.getItem(key);
      return value ? (JSON.parse(value) as T) : null;
    } catch {
      return null;
    }
  }
  private inverseScore(value: number, worst: number) {
    return Math.max(0, Math.round(100 - (value / worst) * 100));
  }
}
