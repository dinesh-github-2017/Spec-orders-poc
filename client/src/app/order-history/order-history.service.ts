import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { OrderFilter, PagedOrders } from './order-history.models';

export interface ExportResult {
  blob: Blob;
  filename: string;
}

@Injectable({ providedIn: 'root' })
export class OrderHistoryService {
  private readonly http = inject(HttpClient);

  search(filter: OrderFilter, page = 1): Observable<PagedOrders> {
    return this.http.get<PagedOrders>(this.ordersUrl(filter.customerId), {
      params: this.buildParams(filter).set('page', String(page))
    });
  }

  export(filter: OrderFilter): Observable<ExportResult> {
    return this.http
      .get(`${this.ordersUrl(filter.customerId)}/export`, {
        params: this.buildParams(filter),
        responseType: 'blob',
        observe: 'response'
      })
      .pipe(
        map(response => ({
          blob: response.body as Blob,
          filename: this.filenameFrom(response.headers.get('Content-Disposition'), filter.customerId)
        }))
      );
  }

  private filenameFrom(disposition: string | null, customerId: string): string {
    const match = disposition?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
    return match ? decodeURIComponent(match[1]) : `orders-${customerId}.csv`;
  }

  private ordersUrl(customerId: string): string {
    return `${environment.apiBaseUrl}/api/customers/${encodeURIComponent(customerId)}/orders`;
  }

  private buildParams(filter: OrderFilter): HttpParams {
    let params = new HttpParams();
    if (filter.from) {
      params = params.set('from', filter.from);
    }
    if (filter.to) {
      params = params.set('to', filter.to);
    }
    if (filter.status) {
      params = params.set('status', filter.status);
    }
    return params;
  }
}
