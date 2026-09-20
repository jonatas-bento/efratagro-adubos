import {
  registerLocaleData,
} from '@angular/common';

import localePt from '@angular/common/locales/pt';

import {
  provideHttpClient,
} from '@angular/common/http';

import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import {
  TestBed,
} from '@angular/core/testing';

import {
  beforeEach,
  describe,
  expect,
  it,
} from 'vitest';

import {
  DeliveriesComponent,
} from './deliveries.component';

registerLocaleData(
  localePt,
  'pt-BR',
);

describe(
  'DeliveriesComponent status contract',
  () => {
    beforeEach(
      async () => {
        await TestBed
          .configureTestingModule({
            imports: [
              DeliveriesComponent,
            ],
            providers: [
              provideHttpClient(),
              provideHttpClientTesting(),
            ],
          })
          .compileComponents();
      },
    );

    it(
      'uses stable status codes instead of display labels',
      () => {
        const fixture =
          TestBed.createComponent(
            DeliveriesComponent,
          );

        const component =
          fixture.componentInstance;

        const http =
          TestBed.inject(
            HttpTestingController,
          );

        const request =
          http.expectOne(
            request =>
              request.url ===
                '/api/deliveries' &&
              request.params.get(
                'pendingOnly',
              ) === 'true',
          );

        request.flush([
          {
            saleId:
              '11111111-1111-1111-1111-111111111111',
            customerName:
              'CLIENTE PENDENTE',
            occurredAtUtc:
              '2026-09-19T15:00:00Z',
            totalQuantity:
              10,
            method:
              'Entrega',
            statusCode:
              1,
            status:
              'AGUARDANDO',
            deliveredAtUtc:
              null,
          },
          {
            saleId:
              '22222222-2222-2222-2222-222222222222',
            customerName:
              'CLIENTE ENTREGUE',
            occurredAtUtc:
              '2026-09-19T14:00:00Z',
            totalQuantity:
              5,
            method:
              'Retirada na loja',
            statusCode:
              2,
            status:
              'FINALIZADA',
            deliveredAtUtc:
              '2026-09-19T16:00:00Z',
          },
        ]);

        http.verify();

        component.showCompleted.set(
          true,
        );

        fixture.detectChanges();

        expect(
          component.pendingCount(),
        ).toBe(1);

        expect(
          component.deliveredCount(),
        ).toBe(1);

        const text =
          fixture.nativeElement
            .textContent;

        expect(
          text,
        ).toContain(
          'AGUARDANDO',
        );

        expect(
          text,
        ).toContain(
          'FINALIZADA',
        );

        const deliveredBadges =
          fixture.nativeElement
            .querySelectorAll(
              '.status-badge.delivered',
            );

        expect(
          deliveredBadges.length,
        ).toBe(1);

        const completeButtons =
          fixture.nativeElement
            .querySelectorAll(
              '.complete-button',
            );

        expect(
          completeButtons.length,
        ).toBe(1);
      },
    );
  },
);
