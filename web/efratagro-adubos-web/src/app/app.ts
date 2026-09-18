import {
  ChangeDetectionStrategy,
  Component,
} from '@angular/core';
import {
  RouterOutlet,
} from '@angular/router';

import {
  AppFooterComponent,
} from './shared/footer/app-footer.component';
import {
  AppNavigationComponent,
} from './shared/navigation/app-navigation.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    AppNavigationComponent,
    AppFooterComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
