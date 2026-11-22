import { Component } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { CustomerRoutePage } from '../shared/customer-route-page';

@Component({
  selector: 'app-product-reviews-page',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './product-reviews-page.component.html',
  styleUrl: './product-reviews-page.component.scss',
})
export class ProductReviewsPageComponent extends CustomerRoutePage {}
