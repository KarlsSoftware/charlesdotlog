import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, Router } from '@angular/router';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
/**
 * Root component — the shell that wraps the whole app.
 * <router-outlet> in the template is replaced with whichever component
 * matches the current URL. The nav bar lives here so it's always visible.
 */
export class AppComponent {
  auth = inject(AuthService);
  router = inject(Router);

  /** Called by the logout button — clears the token and updates the auth signal */
  onLogout() {
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }
}
