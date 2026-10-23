import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { OrderFilter, OrderStatus, PagedOrders } from './order-history.models';
import { OrderHistoryService } from './order-history.service';

export function dateRangeValidator(group: AbstractControl): ValidationErrors | null {
  const from = group.get('from')?.value as string;
  const to = group.get('to')?.value as string;
  return from && to && from > to ? { dateRange: true } : null;
}

@Component({
  selector: 'app-order-history',
  imports: [ReactiveFormsModule],
  templateUrl: './order-history.component.html',
  styleUrl: './order-history.component.scss'
})
export class OrderHistoryComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(OrderHistoryService);

  readonly statuses: OrderStatus[] = ['All', 'Pending', 'Shipped', 'Delivered', 'Cancelled'];

  readonly form = this.fb.nonNullable.group(
    {
      customerId: ['', [Validators.required, Validators.pattern(/\S/)]],
      from: [''],
      to: [''],
      status: ['All' as OrderStatus]
    },
    { validators: dateRangeValidator }
  );

  readonly result = signal<PagedOrders | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly notFound = signal(false);
  readonly searched = signal(false);
  readonly exporting = signal(false);
  readonly exportError = signal<string | null>(null);

  export(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.exporting.set(true);
    this.exportError.set(null);

    this.service.export(this.currentFilter()).subscribe({
      next: ({ blob, filename }) => {
        this.download(blob, filename);
        this.exporting.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.exporting.set(false);
        this.exportError.set(
          err.status === 422
            ? 'Too many orders to export (max 10,000). Narrow the filters.'
            : err.status === 404
              ? 'Customer not found.'
              : 'Export failed. Please try again.'
        );
      }
    });
  }

  private download(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  }

  search(page = 1): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.notFound.set(false);

    this.service.search(this.currentFilter(), page).subscribe({
      next: result => {
        this.result.set(result);
        this.searched.set(true);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.result.set(null);
        this.loading.set(false);
        if (err.status === 404) {
          this.notFound.set(true);
        } else {
          this.error.set('Something went wrong. Please try again.');
        }
      }
    });
  }

  goToPage(page: number): void {
    const total = this.result()?.totalPages ?? 1;
    if (page >= 1 && page <= total) {
      this.search(page);
    }
  }

  private currentFilter(): OrderFilter {
    const value = this.form.getRawValue();
    return {
      customerId: value.customerId.trim(),
      from: value.from || undefined,
      to: value.to || undefined,
      status: value.status
    };
  }
}
