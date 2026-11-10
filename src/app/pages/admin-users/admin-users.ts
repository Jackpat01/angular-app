import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TitleService } from '../../services/title';

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
  selector: 'app-admin-users',
  styleUrl: './admin-users.scss',
  templateUrl: './admin-users.html',
})
export class AdminUsersComponent implements OnInit {
  users: User[] = [
    { id: 1, email: 'john.doe@example.com', name: 'John Doe', role: 'Admin', status: 'active', joinDate: '2023-01-15' },
    { id: 2, email: 'jane.smith@example.com', name: 'Jane Smith', role: 'User', status: 'active', joinDate: '2023-02-20' },
    { id: 3, email: 'bob.wilson@example.com', name: 'Bob Wilson', role: 'Manager', status: 'inactive', joinDate: '2023-03-10' },
    { id: 4, email: 'alice.brown@example.com', name: 'Alice Brown', role: 'User', status: 'active', joinDate: '2023-04-05' },
  ];

  searchTerm = '';
  filteredUsers: User[] = [];
  userForm!: FormGroup;
  showForm = false;

  constructor(
    private titleService: TitleService,
    private fb: FormBuilder,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('User Management');
    this.filteredUsers = this.users;
    this.initializeForm();
  }

  initializeForm(): void {
    this.userForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      name: ['', [Validators.required, Validators.minLength(2)]],
      role: ['User', Validators.required],
    });
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
      this.userForm.reset();
    }
  }

  addUser(): void {
    if (this.userForm.valid) {
      const newUser: User = {
        id: Math.max(...this.users.map(u => u.id), 0) + 1,
        ...this.userForm.value,
        status: 'active',
        joinDate: new Date().toISOString().split('T')[0],
      };
      this.users.push(newUser);
      this.filteredUsers = this.users;
      this.userForm.reset();
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
