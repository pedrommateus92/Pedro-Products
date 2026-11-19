import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { errorMessage } from '../../shared/problem-details';
import { Product } from '../product.model';
import { ProductService } from '../product.service';

export type StockOperation = 'add' | 'remove';

export interface StockDialogData {
  product: Product;
  operation: StockOperation;
}

@Component({
  selector: 'app-stock-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './stock-dialog.html',
  styleUrl: './stock-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StockDialog {
  private readonly productService = inject(ProductService);
  private readonly dialogRef = inject<MatDialogRef<StockDialog, Product>>(MatDialogRef);

  protected readonly data = inject<StockDialogData>(MAT_DIALOG_DATA);
  protected readonly isRemove = this.data.operation === 'remove';
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    quantity: [
      null as number | null,
      [
        Validators.required,
        Validators.min(1),
        Validators.pattern(/^\d+$/),
        ...(this.isRemove ? [Validators.max(this.data.product.stock)] : []),
      ],
    ],
  });

  protected readonly quantity = this.form.controls.quantity;

  protected save(): void {
    if (this.quantity.invalid || this.saving()) {
      this.quantity.markAsTouched();
      return;
    }

    const { id } = this.data.product;
    const quantity = Number(this.quantity.value);
    const request$ = this.isRemove
      ? this.productService.decrementStock(id, quantity)
      : this.productService.addToStock(id, quantity);

    this.saving.set(true);
    this.error.set(null);
    request$.subscribe({
      next: (product) => this.dialogRef.close(product),
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(errorMessage(err));
      },
    });
  }
}
