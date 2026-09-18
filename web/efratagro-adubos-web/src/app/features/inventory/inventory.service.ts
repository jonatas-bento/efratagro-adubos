import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { InventoryItem } from './inventory-item';

@Injectable({
  providedIn: 'root',
})
export class InventoryService {
  private readonly http = inject(HttpClient);

  getInventory(search?: string): Observable<InventoryItem[]> {
    let params = new HttpParams();

    if (search?.trim()) {
      params = params.set('q', search.trim());
    }

    return this.http.get<InventoryItem[]>(
      '/api/inventory',
      { params },
    );
  }
}
