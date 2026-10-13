import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login-page',
  imports: [FormsModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly nickname = signal('');
  protected readonly isLoading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async submit(): Promise<void> {
    const nickname = this.nickname().trim();
    if (!nickname || this.isLoading()) {
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);
    try {
      await this.auth.login(nickname);
      await this.router.navigate(['/chat']);
    } catch (err) {
      this.error.set(
        err instanceof HttpErrorResponse && err.status === 400
          ? (err.error?.detail ?? 'Invalid nickname.')
          : 'Could not reach the server. Is the API running?',
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
