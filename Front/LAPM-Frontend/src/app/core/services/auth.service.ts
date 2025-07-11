import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError, of } from 'rxjs';
import { UserSession } from '../models/user-session.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiBaseUrl = `https://control.lab.local/api/auth`;

  private userSessionSubject = new BehaviorSubject<UserSession | null>(null);
  public userSession$ = this.userSessionSubject.asObservable();

  constructor(private http: HttpClient) { }

  public initializeSession(): Observable<UserSession | null> {
    // Add 'withCredentials: true' to ensure the browser sends authentication cookies/headers.
    return this.http.get<UserSession>(`${this.apiBaseUrl}/session`, { withCredentials: true }).pipe(
      tap(session => this.userSessionSubject.next(session)),
      catchError(() => {
        this.userSessionSubject.next(null);
        return of(null);
      })
    );
  }

  public get currentUser(): UserSession | null {
    return this.userSessionSubject.value;
  }
}