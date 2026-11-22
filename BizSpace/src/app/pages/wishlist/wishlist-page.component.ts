import { Component } from '@angular/core';
import { ProductGridComponent } from '../../components/product-grid/product-grid.component';
import { CustomerRoutePage } from '../shared/customer-route-page';

@Component({
  selector: 'app-wishlist-page',
  standalone: true,
  imports: [ProductGridComponent],
  templateUrl: './wishlist-page.component.html',
  styleUrl: './wishlist-page.component.scss',
})
export class WishlistPageComponent extends CustomerRoutePage {}
