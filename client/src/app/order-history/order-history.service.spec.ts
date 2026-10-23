import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { PagedOrders } from './order-history.models';
import { OrderHistoryService } from './order-history.service';

describe('OrderHistoryService', () => {
  let service: OrderHistoryService;
  let http: HttpTestingController;

  const base = `${environment.apiBaseUrl}/api/customers`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(OrderHistoryService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('builds the URL with from, to, status and page', () => {
    service.search({ customerId: 'C1', from: '2026-05-01', to: '2026-05-31', status: 'Shipped' }, 2).subscribe();

    const req = http.expectOne(r => r.url === `${base}/C1/orders`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('from')).toBe('2026-05-01');
    expect(req.request.params.get('to')).toBe('2026-05-31');
    expect(req.request.params.get('status')).toBe('Shipped');
    expect(req.request.params.get('page')).toBe('2');
    req.flush({});
  });

  it('omits empty from and to params', () => {
    service.search({ customerId: 'C1', status: 'All' }).subscribe();

    const req = http.expectOne(r => r.url === `${base}/C1/orders`);
    expect(req.request.params.has('from')).toBe(false);
    expect(req.request.params.has('to')).toBe(false);
    expect(req.request.params.get('page')).toBe('1');
    req.flush({});
  });

  it('encodes the customer ID in the path', () => {
    service.search({ customerId: 'A/B 1', status: 'All' }).subscribe();

    http.expectOne(r => r.url === `${base}/A%2FB%201/orders`).flush({});
  });

  it('maps the response to PagedOrders', () => {
    const body: PagedOrders = {
      items: [{ orderId: '7', orderDate: '2026-05-31T23:59:00Z', total: 99.99, status: 'Shipped' }],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1
    };
    let result: PagedOrders | undefined;

    service.search({ customerId: 'C1', status: 'All' }).subscribe(r => (result = r));
    http.expectOne(r => r.url === `${base}/C1/orders`).flush(body);

    expect(result).toEqual(body);
  });

  it('export requests /orders/export as a blob and reads the filename', () => {
    let result: { blob: Blob; filename: string } | undefined;

    service.export({ customerId: 'C1', status: 'Shipped' }).subscribe(r => (result = r));

    const req = http.expectOne(r => r.url === `${base}/C1/orders/export`);
    expect(req.request.responseType).toBe('blob');
    expect(req.request.params.get('status')).toBe('Shipped');
    expect(req.request.params.has('page')).toBe(false);
    req.flush(new Blob(['x']), {
      headers: { 'Content-Disposition': 'attachment; filename="orders-C1-20261016.csv"' }
    });

    expect(result?.filename).toBe('orders-C1-20261016.csv');
    expect(result?.blob).toBeInstanceOf(Blob);
  });

  it('export falls back to a default filename', () => {
    let filename = '';

    service.export({ customerId: 'C1', status: 'All' }).subscribe(r => (filename = r.filename));
    http.expectOne(r => r.url === `${base}/C1/orders/export`).flush(new Blob(['x']));

    expect(filename).toBe('orders-C1.csv');
  });

  it('export surfaces a 422', () => {
    let status = 0;

    service.export({ customerId: 'C-BIG', status: 'All' }).subscribe({ error: e => (status = e.status) });
    http
      .expectOne(r => r.url === `${base}/C-BIG/orders/export`)
      .flush(new Blob(['too many']), { status: 422, statusText: 'Unprocessable Entity' });

    expect(status).toBe(422);
  });
});
