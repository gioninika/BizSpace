import { Component } from '@angular/core';
import { CustomerRoutePage } from '../shared/customer-route-page';

@Component({
  selector: 'app-address-book-page',
  standalone: true,
  templateUrl: './address-book-page.component.html',
  styleUrl: './address-book-page.component.scss',
})
export class AddressBookPageComponent extends CustomerRoutePage {}
