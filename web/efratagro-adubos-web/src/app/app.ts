import {
  ChangeDetectionStrategy,
  Component,
} from '@angular/core';

import {
  InventoryComponent,
} from './features/inventory/inventory.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    InventoryComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
