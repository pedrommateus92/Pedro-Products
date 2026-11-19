import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Observable } from 'rxjs';
import { errorMessage, hasStatus, validationErrors } from '../../shared/problem-details';
import { Product } from '../product.model';
import { ProductService } from '../product.service';

export interface ProductFormDialogData {
  /** The product to edit, or null to create a new one. */
  product: Product | null;
}

const twoDecimals = /^\d+(\.\d{1,2})?$/;

@Component({
  selector: 'app-product-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './product-form-dialog.html',
  styleUrl: './product-form-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductFormDialog {
  private readonly productService = inject(ProductService);
  private readonly dialogRef = inject<MatDialogRef<ProductFormDialog, Product>>(MatDialogRef);
  private readonly existing = inject<ProductFormDialogData>(MAT_DIALOG_DATA).product;

  protected readonly isEdit = this.existing !== null;
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly conflict = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: [this.existing?.name ?? '', [Validators.required, Validators.maxLength(200)]],
    description: [this.existing?.description ?? '', Validators.maxLength(1000)],
    price: [
      this.existing?.price ?? (null as number | null),
      [Validators.required, Validators.min(0), Validators.pattern(twoDecimals)],
    ],
    stock: [
      this.existing?.stock ?? (null as number | null),
      [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)],
    ],
  });

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request = {
      name: value.name.trim(),
      description: value.description.trim() || null,
      price: Number(value.price),
      stock: Number(value.stock),
    };

    const save$: Observable<Product> = this.existing
      ? this.productService.update(this.existing.id, {
          ...request,
          rowVersion: this.existing.rowVersion,
        })
      : this.productService.create(request);

    this.saving.set(true);
    this.error.set(null);
    save$.subscribe({
      next: (product) => this.dialogRef.close(product),
      error: (err: unknown) => {
        this.saving.set(false);
        this.applyServerErrors(validationErrors(err));
        this.conflict.set(hasStatus(err, 409));
        this.error.set(
          hasStatus(err, 409)
            ? 'Someone else changed this product while you were editing. Close and reopen it to get the latest values.'
            : errorMessage(err),
        );
      },
    });
  }

  private applyServerErrors(errors: Record<string, string[]>): void {
    for (const [field, messages] of Object.entries(errors)) {
      this.form.get(field)?.setErrors({ server: messages.join(' ') });
    }
  }
}
