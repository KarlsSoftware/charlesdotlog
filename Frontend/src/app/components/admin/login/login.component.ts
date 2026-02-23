import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  password = '';
  error = '';

  constructor(private auth: AuthService, private router: Router) {}

  /**
   * Called on form submit.
   * .subscribe() is used here instead of async pipe because we need to
   * imperatively navigate or show an error after the HTTP call completes.
   */
  onLogin() {
    this.error = '';
    this.auth.login(this.password).subscribe({
      next: () => this.router.navigate(['/admin']),    // success → go to dashboard
      error: () => (this.error = 'Falsches Passwort'), // 401 → show inline error
    });
  }
}
