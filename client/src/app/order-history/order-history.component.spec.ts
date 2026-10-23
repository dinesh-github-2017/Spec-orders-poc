import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { OrderHistoryComponent } from './order-history.component';
import { PagedOrders } from './order-history.models';
import { OrderHistoryService } from './order-history.service';

describe('OrderHistoryComponent', () => {
  const page = (overrides: Partial<PagedOrders> = {}): PagedOrders => ({
    items: [
      { orderId: '7', orderDate: '2026-05-31T23:59:00Z', total: 99.99, status: 'Shipped' },
      { orderId: '6', orderDate: '2026-05-30T08:00:00Z', total: 10, status: 'Pending' }
    ],
    page: 1,
    pageSize: 20,
    totalCount: 45,
    totalPages: 3,
    ...overrides
  });

  let search: ReturnType<typeof vi.fn>;
  let exportCsv: ReturnType<typeof vi.fn>;

  function create() {
    TestBed.configureTestingModule({
      imports: [OrderHistoryComponent],
      providers: [{ provide: OrderHistoryService, useValue: { search, export: exportCsv } }]
    });
    const fixture = TestBed.createComponent(OrderHistoryComponent);
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ReturnType<typeof create>): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  beforeEach(() => {
    search = vi.fn().mockReturnValue(of(page()));
    exportCsv = vi.fn().mockReturnValue(of({ blob: new Blob(['x']), filename: 'orders-C1-20261016.csv' }));
  });

  it('disables Export CSV until the customer ID is valid', () => {
    const fixture = create();
    const button = () =>
      Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).find(b =>
        b.textContent?.includes('Export CSV')
      ) as HTMLButtonElement;

    expect(button().disabled).toBe(true);
    fixture.componentInstance.form.patchValue({ customerId: 'C1' });
    fixture.detectChanges();

    expect(button().disabled).toBe(false);
  });

  it('exports with the current filters and triggers a download', () => {
    const createUrl = vi.fn().mockReturnValue('blob:test');
    const revokeUrl = vi.fn();
    vi.stubGlobal('URL', { createObjectURL: createUrl, revokeObjectURL: revokeUrl });
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);

    const fixture = create();
    fixture.componentInstance.form.patchValue({ customerId: 'C1', status: 'Shipped' });
    fixture.componentInstance.export();

    expect(exportCsv).toHaveBeenCalledWith(expect.objectContaining({ customerId: 'C1', status: 'Shipped' }));
    expect(createUrl).toHaveBeenCalled();
    expect(click).toHaveBeenCalled();
    expect(revokeUrl).toHaveBeenCalledWith('blob:test');

    click.mockRestore();
    vi.unstubAllGlobals();
  });

  it('shows the too-many-orders message on 422', () => {
    exportCsv.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 422 })));
    const fixture = create();
    fixture.componentInstance.form.patchValue({ customerId: 'C-BIG' });
    fixture.componentInstance.export();
    fixture.detectChanges();

    expect(text(fixture)).toContain('Too many orders to export (max 10,000). Narrow the filters.');
  });

  it('flags a missing customer ID and does not search', () => {
    const fixture = create();
    const button = fixture.nativeElement.querySelector('button[type=submit]') as HTMLButtonElement;

    expect(button.disabled).toBe(true);
    fixture.componentInstance.search();
    fixture.detectChanges();

    expect(search).not.toHaveBeenCalled();
    expect(text(fixture)).toContain('Customer ID is required.');
  });

  it('renders rows with order ID, date, total and status', () => {
    const fixture = create();
    fixture.componentInstance.form.patchValue({ customerId: 'C1' });
    fixture.componentInstance.search();
    fixture.detectChanges();

    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('7');
    expect(rows[0].textContent).toContain('2026-05-31T23:59:00Z');
    expect(rows[0].textContent).toContain('99.99');
    expect(rows[0].textContent).toContain('Shipped');
  });

  it('shows paging controls and requests the next page', () => {
    const fixture = create();
    fixture.componentInstance.form.patchValue({ customerId: 'C1' });
    fixture.componentInstance.search();
    fixture.detectChanges();

    expect(text(fixture)).toContain('Page 1 of 3');
    fixture.componentInstance.goToPage(2);

    expect(search).toHaveBeenLastCalledWith(expect.objectContaining({ customerId: 'C1' }), 2);
  });

  it('shows "No orders found" when the result is empty', () => {
    search.mockReturnValue(of(page({ items: [], totalCount: 0, totalPages: 0 })));
    const fixture = create();
    fixture.componentInstance.form.patchValue({ customerId: 'C-EMPTY' });
    fixture.componentInstance.search();
    fixture.detectChanges();

    expect(text(fixture)).toContain('No orders found');
  });

  it('shows "Customer not found" on 404', () => {
    search.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    const fixture = create();
    fixture.componentInstance.form.patchValue({ customerId: 'NOPE' });
    fixture.componentInstance.search();
    fixture.detectChanges();

    expect(text(fixture)).toContain('Customer not found');
  });

  it('flags from after to', () => {
    const fixture = create();
    fixture.componentInstance.form.patchValue({ customerId: 'C1', from: '2026-06-02', to: '2026-06-01' });
    fixture.detectChanges();

    expect(text(fixture)).toContain("'From' date must be on or before 'To' date.");
    expect(fixture.componentInstance.form.invalid).toBe(true);
  });
});
