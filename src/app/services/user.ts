import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface UserInfo {
  email: string;
  role: string;
  providerType: string;
  projectName: string;
  isAuthenticated: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private userSubject = new BehaviorSubject<UserInfo>({
    email: 'user@example.com',
    role: 'User',
    providerType: 'Standard',
    projectName: 'Main Project',
    isAuthenticated: false,
  });

  user$ = this.userSubject.asObservable();

  getUser(): UserInfo {
    return this.userSubject.value;
  }

  setUser(user: UserInfo): void {
    this.userSubject.next(user);
  }

  setAuthenticated(isAuth: boolean): void {
    const current = this.userSubject.value;
    this.userSubject.next({ ...current, isAuthenticated: isAuth });
  }

  logout(): void {
    this.userSubject.next({
      email: '',
      role: '',
      providerType: '',
      projectName: '',
      isAuthenticated: false,
    });
  }
}
