import { Component } from '@angular/core';
import { CustomerRoutePage } from '../shared/customer-route-page';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  templateUrl: './profile-page.component.html',
  styleUrl: './profile-page.component.scss',
})
export class ProfilePageComponent extends CustomerRoutePage {}
