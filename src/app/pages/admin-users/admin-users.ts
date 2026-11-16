import {Component, OnInit, inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {BaseFormComponent} from '../../shared/base-form.component';

interface User {
  id: number;
  email: string;
  name: string;
  role: string;
  status: 'active' | 'inactive';
  joinDate: string;
}

@Component({
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  selector: 'ohfi-admin-users',
  styleUrl: './admin-users.css',
  templateUrl: './admin-users.html',
})
export class AdminUsersComponent extends BaseFormComponent implements OnInit {
  private modalService = inject(NgbModal);
  users: User[] = [
    {id: 1, email: 'john.doe@example.com', name: 'John Doe', role: 'Admin', status: 'active', joinDate: '2023-01-15'},
    {
      id: 2,
      email: 'jane.smith@example.com',
      name: 'Jane Smith',
      role: 'User',
      status: 'active',
      joinDate: '2023-02-20'
    },
    {
      id: 3,
      email: 'bob.wilson@example.com',
      name: 'Bob Wilson',
      role: 'Manager',
      status: 'inactive',
      joinDate: '2023-03-10'
    },
    {
      id: 4,
      email: 'alice.brown@example.com',
      name: 'Alice Brown',
      role: 'User',
      status: 'active',
      joinDate: '2023-04-05'
    },
  ];

  searchTerm = '';
  filteredUsers: User[] = [];
  showForm = false;

  protected getPageTitle(): string {
    return 'User Management';
  }

  protected buildForm(): FormGroup {
    return this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      name: ['', [Validators.required, Validators.minLength(2)]],
      role: ['User', Validators.required],
    });
  }

  override ngOnInit(): void {
    super.ngOnInit();
    this.filteredUsers = this.users;
  }

  filterUsers(): void {
    if (!this.searchTerm.trim()) {
      this.filteredUsers = this.users;
    } else {
      const term = this.searchTerm.toLowerCase();
      this.filteredUsers = this.users.filter(
        u => u.email.toLowerCase().includes(term) || u.name.toLowerCase().includes(term)
      );
    }
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.form.reset();
    }
  }

  addUser(): void {
    if (this.form.valid) {
      const newUser: User = {
        id: Math.max(...this.users.map(u => u.id), 0) + 1,
        ...this.form.value,
        status: 'active',
        joinDate: new Date().toISOString().split('T')[0],
      };
      this.users.push(newUser);
      this.filteredUsers = this.users;
      this.form.reset();
      this.showForm = false;
    }
  }

  deleteUser(id: number): void {
    if (confirm('Are you sure you want to delete this user?')) {
      this.users = this.users.filter(u => u.id !== id);
      this.filteredUsers = this.filteredUsers.filter(u => u.id !== id);
    }
  }

  toggleUserStatus(id: number): void {
    const user = this.users.find(u => u.id === id);
    if (user) {
      user.status = user.status === 'active' ? 'inactive' : 'active';
    }
  }
}
