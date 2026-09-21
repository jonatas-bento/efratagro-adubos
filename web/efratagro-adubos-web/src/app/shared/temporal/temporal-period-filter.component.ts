import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
} from '@angular/core';
import {
  FormsModule,
} from '@angular/forms';

export interface TemporalPeriod {
  from: string;
  to: string;
}

@Component({
  selector: 'app-temporal-period-filter',
  standalone: true,
  imports: [
    FormsModule,
  ],
  template: `
    <section
      class="temporal-filter"
      aria-label="Filtro por período"
    >
      <div class="temporal-filter-heading">
        <div>
          <span class="eyebrow">
            Filtro temporal
          </span>

          <strong>
            {{ title }}
          </strong>
        </div>

        @if (from || to) {
          <span class="active-badge">
            Ativo
          </span>
        }
      </div>

      <div class="temporal-fields">
        <label>
          <span>De</span>

          <input
            type="date"
            [(ngModel)]="draftFrom"
          >
        </label>

        <label>
          <span>Até</span>

          <input
            type="date"
            [(ngModel)]="draftTo"
          >
        </label>

        <div class="temporal-actions">
          <button
            type="button"
            class="apply-button"
            (click)="apply()"
          >
            Aplicar
          </button>

          <button
            type="button"
            class="clear-button"
            (click)="clear()"
            [disabled]="
              !draftFrom &&
              !draftTo &&
              !from &&
              !to
            "
          >
            Limpar
          </button>
        </div>
      </div>

      @if (error) {
        <small class="temporal-error">
          {{ error }}
        </small>
      }
    </section>
  `,
  styles: [`
    :host {
      display: block;
    }

    .temporal-filter {
      display: grid;
      gap: .85rem;
      margin: 0 0 1rem;
      padding: 1rem;
      border: 1px solid rgba(0, 0, 0, .09);
      border-radius: .85rem;
      background: rgba(255, 255, 255, .55);
    }

    .temporal-filter-heading {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
    }

    .temporal-filter-heading > div {
      display: grid;
      gap: .2rem;
    }

    .eyebrow {
      font-size: .72rem;
      font-weight: 700;
      letter-spacing: .08em;
      text-transform: uppercase;
      opacity: .62;
    }

    .active-badge {
      padding: .25rem .55rem;
      border-radius: 999px;
      font-size: .75rem;
      font-weight: 700;
      background: rgba(0, 0, 0, .07);
    }

    .temporal-fields {
      display: grid;
      grid-template-columns:
        repeat(2, minmax(0, 1fr))
        auto;
      align-items: end;
      gap: .75rem;
    }

    label {
      display: grid;
      gap: .35rem;
      font-size: .8rem;
      font-weight: 600;
    }

    input {
      min-height: 2.6rem;
      padding: .55rem .65rem;
      border: 1px solid rgba(0, 0, 0, .14);
      border-radius: .6rem;
      background: #fff;
      font: inherit;
    }

    .temporal-actions {
      display: flex;
      gap: .45rem;
    }

    button {
      min-height: 2.6rem;
      padding: .55rem .8rem;
      border-radius: .6rem;
      cursor: pointer;
      font: inherit;
      font-weight: 700;
    }

    .apply-button {
      border: 0;
      background: #1f4f35;
      color: #fff;
    }

    .clear-button {
      border: 1px solid rgba(0, 0, 0, .14);
      background: transparent;
    }

    button:disabled {
      cursor: default;
      opacity: .45;
    }

    .temporal-error {
      font-weight: 600;
      color: #a52828;
    }

    @media (max-width: 720px) {
      .temporal-fields {
        grid-template-columns: 1fr;
      }

      .temporal-actions {
        width: 100%;
      }

      .temporal-actions button {
        flex: 1;
      }
    }
  `],
})
export class TemporalPeriodFilterComponent
    implements OnChanges {
  @Input() title = 'Período';

  @Input() from = '';

  @Input() to = '';

  @Output()
  readonly periodChange =
    new EventEmitter<TemporalPeriod>();

  draftFrom = '';

  draftTo = '';

  error: string | null = null;

  ngOnChanges(
    changes: SimpleChanges,
  ): void {
    if (
      changes['from'] ||
      changes['to']
    ) {
      this.draftFrom =
        this.from ?? '';

      this.draftTo =
        this.to ?? '';
    }
  }

  apply(): void {
    this.error = null;

    if (
      this.draftFrom &&
      this.draftTo &&
      this.draftFrom >
        this.draftTo
    ) {
      this.error =
        'A data inicial não pode ser posterior à data final.';

      return;
    }

    this.periodChange.emit({
      from: this.draftFrom,
      to: this.draftTo,
    });
  }

  clear(): void {
    this.error = null;

    this.draftFrom = '';
    this.draftTo = '';

    this.periodChange.emit({
      from: '',
      to: '',
    });
  }
}
