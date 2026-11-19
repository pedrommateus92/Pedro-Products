import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  Observable,
  Subject,
  catchError,
  debounceTime,
  filter,
  map,
  merge,
  of,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/confirm-dialog/confirm-dialog';
import { errorMessage, hasStatus } from '../../shared/problem-details';
import {
  ProductFormDialog,
  ProductFormDialogData,
} from '../product-form-dialog/product-form-dialog';
import { Product } from '../product.model';
import { ProductService } from '../product.service';
import { StockDialog, StockDialogData, StockOperation } from '../stock-dialog/stock-dialog';

interface Filters {
  name: string;
  min: number | null;
  max: number | null;
}

function minNotAboveMax(group: AbstractControl): ValidationErrors | null {
  const { min, max } = group.value as Filters;
  return min !== null && max !== null && min > max ? { range: true } : null;
}

@Component({
  selector: 'app-product-list',
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatTableModule,
    MatTooltipModule,
  ],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductList {
  private readonly productService = inject(ProductService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly reload$ = new Subject<void>();

  protected readonly columns = ['id', 'name', 'description', 'price', 'stock', 'actions'];
  protected readonly pageSizeOptions = [5, 10, 25];

  protected readonly filters = inject(NonNullableFormBuilder).group(
    {
      name: '',
      min: [null as number | null, [Validators.min(0), Validators.pattern(/^\d+$/)]],
      max: [null as number | null, [Validators.min(0), Validators.pattern(/^\d+$/)]],
    },
    { validators: minNotAboveMax },
  );

  protected readonly products = signal<Product[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  protected readonly pagedProducts = computed(() => {
    const start = this.pageIndex() * this.pageSize();
    return this.products().slice(start, start + this.pageSize());
  });

  protected readonly hasFilters = computed(() => this.activeFilters() !== null);
  private readonly activeFilters = signal<Filters | null>(null);

  constructor() {
    const filterChanges$ = this.filters.valueChanges.pipe(debounceTime(300));

    merge(filterChanges$, this.reload$)
      .pipe(
        startWith(null),
        map(() => this.filters),
        filter((form) => form.valid),
        map((form) => normalize(form.getRawValue())),
        tap((filters) => {
          this.activeFilters.set(filters);
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap((filters) =>
          this.query(filters).pipe(
            catchError((err: unknown) => {
              this.error.set(errorMessage(err, 'Could not load products.'));
              return of<Product[] | null>(null);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((products) => {
        this.loading.set(false);
        if (products) {
          this.products.set(products);
          this.clampPage(products.length);
        }
      });
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }

  protected clearFilters(): void {
    this.filters.reset({ name: '', min: null, max: null });
  }

  protected reload(): void {
    this.reload$.next();
  }

  protected openCreate(): void {
    this.openForm({ product: null }, (p) => `Created "${p.name}" (ID ${p.id}).`);
  }

  protected openEdit(product: Product): void {
    this.openForm({ product }, (p) => `Saved "${p.name}".`);
  }

  protected openStock(product: Product, operation: StockOperation): void {
    this.dialog
      .open<StockDialog, StockDialogData, Product>(StockDialog, { data: { product, operation } })
      .afterClosed()
      .subscribe((updated) => {
        if (updated) {
          this.notify(`"${updated.name}" now has ${updated.stock} in stock.`);
        }
        // Reload even after a cancel: a failed attempt may mean the stock changed elsewhere.
        this.reload();
      });
  }

  protected confirmDelete(product: Product): void {
    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
        data: {
          title: 'Delete product',
          message: `Delete "${product.name}" (ID ${product.id})? This cannot be undone.`,
          confirmLabel: 'Delete',
        },
      })
      .afterClosed()
      .pipe(
        filter(Boolean),
        switchMap(() => this.productService.delete(product.id)),
      )
      .subscribe({
        next: () => {
          this.notify(`Deleted "${product.name}".`);
          this.reload();
        },
        error: (err: unknown) => {
          this.notify(
            hasStatus(err, 404) ? `"${product.name}" was already deleted.` : errorMessage(err),
          );
          this.reload();
        },
      });
  }

  private openForm(data: ProductFormDialogData, message: (product: Product) => string): void {
    this.dialog
      .open<ProductFormDialog, ProductFormDialogData, Product>(ProductFormDialog, { data })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify(message(saved));
        }
        // Reload even after a cancel, so a product changed elsewhere reopens with fresh values.
        this.reload();
      });
  }

  private query(filters: Filters | null): Observable<Product[]> {
    if (!filters) {
      return this.productService.getAll();
    }
    const { name, min, max } = filters;
    const low = min ?? 0;
    const high = max ?? 2147483647;
    if (name) {
      // The API searches by name only; the stock range is applied to those results here.
      return this.productService
        .searchByName(name)
        .pipe(map((products) => products.filter((p) => p.stock >= low && p.stock <= high)));
    }
    return this.productService.getByStockLevel(low, high);
  }

  private clampPage(total: number): void {
    const lastPage = Math.max(0, Math.ceil(total / this.pageSize()) - 1);
    if (this.pageIndex() > lastPage) {
      this.pageIndex.set(lastPage);
    }
  }

  private notify(message: string): void {
    this.snackBar.open(message, 'Close', { duration: 4000 });
  }
}

function normalize(value: {
  name: string;
  min: number | null;
  max: number | null;
}): Filters | null {
  const name = value.name.trim();
  const min = value.min === null || String(value.min) === '' ? null : Number(value.min);
  const max = value.max === null || String(value.max) === '' ? null : Number(value.max);
  return name || min !== null || max !== null ? { name, min, max } : null;
}
