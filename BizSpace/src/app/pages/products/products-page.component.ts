import { Component } from '@angular/core';
import { ProductFiltersComponent } from '../../components/product-filters/product-filters.component';
import { ProductGridComponent } from '../../components/product-grid/product-grid.component';
import { CustomerRoutePage } from '../shared/customer-route-page';

@Component({
  selector: 'app-products-page',
  standalone: true,
  imports: [ProductGridComponent, ProductFiltersComponent],
  templateUrl: './products-page.component.html',
  styleUrl: './products-page.component.scss',
})
export class ProductsPageComponent extends CustomerRoutePage {}
