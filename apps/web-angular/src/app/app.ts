import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { RecoveryEngineService } from './core/services/recovery-engine.service';
@Component({ selector: 'app-root', imports: [RouterOutlet], template: '<router-outlet />' })
export class App { private readonly recovery = inject(RecoveryEngineService); }
