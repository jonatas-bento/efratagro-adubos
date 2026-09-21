import {
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
} from '@angular/core';
import {
  FormsModule,
} from '@angular/forms';

import {
  DataQualityService,
} from '../../core/services/data-quality.service';

@Component({
  selector: 'app-temporal-as-of-filter',
  standalone: true,
  imports: [
    FormsModule,
  ],
  template: `
    <section
      class="temporal-filter"
      aria-label="Posição histórica do estoque"
    >
      <div>
        <span class="eyebrow">
          Posição do estoque
        </span>

        <strong>
          {{
            value
              ? 'Fechamento histórico'
              : 'Posição atual'
          }}
        </strong>
      </div>

      <div class="temporal-fields">
        <label>
          <span>Estoque em</span>

          <input
            type="date"
            [(ngModel)]="draft"
            [min]="minimumDate || null"
          >
        </label>

        <button
          type="button"
          class="apply-button"
          (click)="apply()"
        >
          Consultar
        </button>

        <button
          type="button"
          class="clear-button"
          (click)="clear()"
          [disabled]="!draft && !value"
        >
          Hoje
        </button>
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
      display: flex;
      justify-content: space-between;
      align-items: end;
      gap: 1rem;
      margin: 0 0 1rem;
      padding: 1rem;
      border: 1px solid rgba(0, 0, 0, .09);
      border-radius: .85rem;
      background: rgba(255, 255, 255, .55);
    }

    .temporal-filter > div:first-child {
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

    .temporal-fields {
      display: flex;
      align-items: end;
      gap: .55rem;
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
      grid-column: 1 / -1;
      font-weight: 600;
      color: #a52828;
    }

    @media (max-width: 720px) {
      .temporal-filter {
        align-items: stretch;
        flex-direction: column;
      }

      .temporal-fields {
        display: grid;
        grid-template-columns: 1fr 1fr;
      }

      label {
        grid-column: 1 / -1;
      }
    }
  `],
})
export class TemporalAsOfFilterComponent
    implements OnInit {
  private internalValue = '';

  @Input()
  set value(value: string) {
    this.internalValue =
      value ?? '';

    this.draft =
      this.internalValue;
  }

  get value(): string {
    return this.internalValue;
  }

  @Output()
  readonly dateChange =
    new EventEmitter<string>();

  draft = '';

  minimumDate = '';

  error: string | null = null;

  constructor(
    private readonly dataQuality:
      DataQualityService,
  ) {
  }

  ngOnInit(): void {
    this.dataQuality
      .getDataQuality()
      .subscribe(data => {
        this.minimumDate =
          data
            .inventoryHistoryAvailableFrom ??
          '';
      });
  }

  apply(): void {
    this.error = null;

    if (
      this.draft &&
      this.minimumDate &&
      this.draft <
        this.minimumDate
    ) {
      this.error =
        'O histórico físico está disponível ' +
        `a partir de ${
          this.formatDate(
            this.minimumDate,
          )
        }.`;

      return;
    }

    this.dateChange.emit(
      this.draft,
    );
  }

  clear(): void {
    this.error = null;
    this.draft = '';

    this.dateChange.emit('');
  }

  private formatDate(
    value: string,
  ): string {
    const [
      year,
      month,
      day,
    ] = value.split('-');

    return `${day}/${month}/${year}`;
  }
}
