import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DisruptionEngineService } from '../../core/services/disruption-engine.service';
import { DisruptionScenario } from '../../core/models/disruption.model';
interface Preset {
  name: string;
  description: string;
  icon: string;
  scenario: DisruptionScenario;
}
@Component({
  imports: [CommonModule, RouterLink],
  templateUrl: './scenario-lab.page.html',
  styleUrl: './scenario-lab.page.scss',
})
export class ScenarioLabPage {
  readonly engine = inject(DisruptionEngineService);
  readonly running = signal('');
  readonly confirmReset = signal(false);
  readonly presets: Preset[] = [
    {
      name: 'Toronto Thunderstorm',
      description:
        'Severe convective weather impacts Pearson departures and AC103’s downstream rotation.',
      icon: 'ϟ',
      scenario: {
        type: 'Severe weather',
        severity: 'Critical',
        airport: 'YYZ',
        flightId: 'AC103',
        durationMinutes: 120,
      },
    },
    {
      name: 'Aircraft Mechanical Issue',
      description: 'An unscheduled inspection removes AC882’s assigned widebody from service.',
      icon: '⚙',
      scenario: {
        type: 'Aircraft maintenance',
        severity: 'Critical',
        airport: 'YYZ',
        flightId: 'AC882',
        durationMinutes: 95,
      },
    },
    {
      name: 'Montréal Runway Closure',
      description:
        'A temporary runway closure constrains arrivals and propagates delay from AC791.',
      icon: '∥',
      scenario: {
        type: 'Runway closure',
        severity: 'High',
        airport: 'YUL',
        flightId: 'AC791',
        durationMinutes: 75,
      },
    },
    {
      name: 'Calgary Crew Shortage',
      description: 'Crew availability places AC156 near its legal duty-time threshold.',
      icon: '♧',
      scenario: {
        type: 'Crew timing issue',
        severity: 'High',
        airport: 'YYC',
        flightId: 'AC156',
        durationMinutes: 60,
      },
    },
  ];
  run(preset: Preset) {
    this.running.set(preset.name);
    setTimeout(() => {
      this.engine.runScenario(preset.name, preset.description, preset.scenario);
      this.running.set('');
    }, 350);
  }
  stress() {
    this.running.set('stress');
    setTimeout(() => { this.engine.runNetworkStressTest(); this.running.set(''); }, 350);
  }
  replay(index: number) {
    const run = this.engine.scenarioRuns()[index];
    if (run) this.engine.replay(run);
  }
  reset() {
    this.engine.resetSimulation();
    this.confirmReset.set(false);
  }
}
