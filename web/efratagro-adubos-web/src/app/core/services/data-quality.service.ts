import {
  HttpClient,
} from '@angular/common/http';
import {
  inject,
  Injectable,
} from '@angular/core';
import {
  Observable,
  shareReplay,
} from 'rxjs';

import {
  DataQuality,
} from '../models/data-quality';

@Injectable({
  providedIn: 'root',
})
export class DataQualityService {
  private readonly http =
    inject(HttpClient);

  private readonly dataQuality$ =
    this.http
      .get<DataQuality>(
        '/api/data-quality',
      )
      .pipe(
        shareReplay({
          bufferSize: 1,
          refCount: false,
        }),
      );

  getDataQuality():
      Observable<DataQuality> {
    return this.dataQuality$;
  }
}
