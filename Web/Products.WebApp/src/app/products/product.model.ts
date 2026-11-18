export interface Product {
  id: number;
  name: string;
  description: string | null;
  price: number;
  stock: number;
  rowVersion: string;
}

export interface CreateProductRequest {
  name: string;
  description: string | null;
  price: number;
  stock: number;
}

export interface UpdateProductRequest extends CreateProductRequest {
  rowVersion: string;
}
