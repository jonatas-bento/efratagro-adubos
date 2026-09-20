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
  PurchasesComponent,
} from '../purchases/purchases.component';

import {
  SalesComponent,
} from '../sales/sales.component';

describe(
  'Inventory operational visibility',
  () => {
    beforeEach(
      async () => {
        await TestBed
          .configureTestingModule({
            imports: [
              SalesComponent,
              PurchasesComponent,
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
      'keeps inactive stock out of new sales',
      () => {
        const fixture =
          TestBed.createComponent(
            SalesComponent,
          );

        const component =
          fixture.componentInstance;

        const http =
          TestBed.inject(
            HttpTestingController,
          );

        flushPendingRequests(http);

        component.inventory.set([
          {
            productId: 'active-stock',
            productName: 'ATIVO COM ESTOQUE',
            supplierName: 'FORNECEDOR',
            isActive: true,
            quantity: 10,
          },
          {
            productId: 'inactive-stock',
            productName: 'INATIVO COM ESTOQUE',
            supplierName: 'FORNECEDOR',
            isActive: false,
            quantity: 5,
          },
          {
            productId: 'active-empty',
            productName: 'ATIVO SEM ESTOQUE',
            supplierName: 'FORNECEDOR',
            isActive: true,
            quantity: 0,
          },
        ]);

        expect(
          component
            .availableProducts()
            .map(
              product =>
                product.productId,
            ),
        ).toEqual([
          'active-stock',
        ]);
      },
    );

    it(
      'keeps inactive products out of new purchases',
      () => {
        const fixture =
          TestBed.createComponent(
            PurchasesComponent,
          );

        const component =
          fixture.componentInstance;

        const http =
          TestBed.inject(
            HttpTestingController,
          );

        flushPendingRequests(http);

        component.suppliers.set([
          {
            id: 'supplier-1',
            name: 'FORNECEDOR',
          },
        ]);

        component.inventory.set([
          {
            productId: 'active-product',
            productName: 'ATIVO',
            supplierName: 'FORNECEDOR',
            isActive: true,
            quantity: 10,
          },
          {
            productId: 'inactive-product',
            productName: 'INATIVO',
            supplierName: 'FORNECEDOR',
            isActive: false,
            quantity: 5,
          },
          {
            productId: 'other-supplier',
            productName: 'OUTRO',
            supplierName: 'OUTRO FORNECEDOR',
            isActive: true,
            quantity: 10,
          },
        ]);

        component.form.controls
          .supplierId
          .setValue(
            'supplier-1',
          );

        expect(
          component
            .productsForSupplier()
            .map(
              product =>
                product.productId,
            ),
        ).toEqual([
          'active-product',
        ]);
      },
    );
  },
);

function flushPendingRequests(
  http: HttpTestingController,
): void {
  for (
    const request
    of http.match(() => true)
  ) {
    request.flush([]);
  }

  http.verify();
}
