import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
} from '@angular/core';

import {
  DataQuality,
} from '../../core/models/data-quality';
import {
  DataQualityService,
} from '../../core/services/data-quality.service';

export type DataQualityContext =
  'commercial' |
  'inventory';

@Component({
  selector: 'app-data-quality-banner',
  standalone: true,
  template: `
    @if (data) {
      <section
        class="quality-banner"
        aria-label="Qualidade temporal dos dados"
      >
        @if (
          data.inventoryHistoryAvailableFrom;
          as baseline
        ) {
          <div class="quality-line">
            <span class="quality-icon">
              ◷
            </span>

            <span>
              Histórico físico disponível desde
              <strong>
                {{ formatDate(baseline) }}
              </strong>.
            </span>
          </div>
        }

        @if (!data.operationalTrustedFrom) {
          <div
            class="quality-line quality-warning"
          >
            <span class="quality-icon">
              △
            </span>

            <span>
              A data de confiança operacional
              ainda não foi definida.
            </span>
          </div>
        } @else if (
          includesUntrustedData()
        ) {
          <div
            class="quality-line quality-warning"
          >
            <span class="quality-icon">
              △
            </span>

            <span>
              Esta visão inclui dados anteriores
              à reconciliação operacional de
              <strong>
                {{
                  formatDate(
                    data.operationalTrustedFrom
                  )
                }}
              </strong>.
            </span>
          </div>
        } @else {
          <div
            class="quality-line quality-ok"
          >
            <span class="quality-icon">
              ✓
            </span>

            <span>
              Dados dentro da faixa reconciliada
              desde
              <strong>
                {{
                  formatDate(
                    data.operationalTrustedFrom
                  )
                }}
              </strong>.
            </span>
          </div>
        }
      </section>
    }
  `,
  styles: [`
    :host {
      display: block;
    }

    .quality-banner {
      display: grid;
      gap: .55rem;
      margin: 0 0 1rem;
      padding: .9rem 1rem;
      border: 1px solid rgba(0, 0, 0, .09);
      border-radius: .8rem;
      background: rgba(255, 255, 255, .55);
      font-size: .83rem;
    }

    .quality-line {
      display: flex;
      align-items: flex-start;
      gap: .55rem;
      line-height: 1.4;
    }

    .quality-icon {
      flex: 0 0 auto;
      font-weight: 800;
    }

    .quality-warning {
      color: #805d13;
    }

    .quality-ok {
      color: #285a3a;
    }
  `],
  changeDetection:
    ChangeDetectionStrategy.OnPush,
})
export class DataQualityBannerComponent
    implements OnInit {
  private readonly dataQualityService:
    DataQualityService;

  @Input()
  context: DataQualityContext =
    'commercial';

  @Input() from = '';

  @Input() to = '';

  @Input() asOf = '';

  data: DataQuality | null = null;

  constructor(
    dataQualityService:
      DataQualityService,
  ) {
    this.dataQualityService =
      dataQualityService;
  }

  ngOnInit(): void {
    this.dataQualityService
      .getDataQuality()
      .subscribe(data => {
        this.data = data;
      });
  }

  includesUntrustedData(): boolean {
    const trustedFrom =
      this.data
        ?.operationalTrustedFrom;

    if (!trustedFrom) {
      return false;
    }

    if (
      this.context ===
      'inventory'
    ) {
      /*
       * asOf vazio representa
       * a posição atual.
       */
      return Boolean(
        this.asOf &&
        this.asOf <
          trustedFrom,
      );
    }

    /*
     * Comercial sem data inicial
     * inclui o legado.
     */
    if (!this.from) {
      return true;
    }

    return (
      this.from <
      trustedFrom
    );
  }

  formatDate(
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
