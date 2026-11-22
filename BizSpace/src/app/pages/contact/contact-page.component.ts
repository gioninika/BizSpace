import { Component } from '@angular/core';
import { CustomerRoutePage } from '../shared/customer-route-page';

@Component({
  selector: 'app-contact-page',
  standalone: true,
  templateUrl: './contact-page.component.html',
  styleUrl: './contact-page.component.scss',
})
export class ContactPageComponent extends CustomerRoutePage {}
