import {Component, OnInit, inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {BaseListCrudComponent} from '../../shared/base-list-crud.component';
import {DataTableComponent, TableColumn, TableAction} from '../../shared/data-table.component';

interface User {
  id: number;
  email: string;
  name: string;
  role: string;
  status: 'active' | 'inactive';
  joinDate: string;
}

@Component({
  imports: [CommonModule, ReactiveFormsModule, FormsModule, DataTableComponent],
  selector: 'ohfi-admin-users',
  styleUrl: './admin-users.css',
  templateUrl: './admin-users.html',
})
export class AdminUsersComponent extends BaseListCrudComponent<User> implements OnInit {
  private modalService = inject(NgbModal);

  userColumns: TableColumn[] = [
    { key: 'email', label: 'Email' },
    { key: 'name', label: 'Name' },
    { key: 'role', label: 'Role' },
    { key: 'status', label: 'Status', format: (val) => (val as string)?.toUpperCase() },
    { key: 'joinDate', label: 'Join Date' },
  ];

  userActions: TableAction[] = [
    {
      label: 'Deactivate',
      class: 'btn btn-sm btn-warning me-2',
      disabled: (user) => (user as User).status === 'inactive',
      handler: (id) => this.toggleUserStatus(id as number)
    },
    {
      label: 'Activate',
      class: 'btn btn-sm btn-success me-2',
      disabled: (user) => (user as User).status === 'active',
      handler: (id) => this.toggleUserStatus(id as number)
    },
    {
      label: 'Delete',
      class: 'btn btn-sm btn-danger',
      handler: (id) => this.delete(id)
    }
  ];

  constructor() {
    super();
    this.items = [
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
  }

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

  protected matchesSearchTerm(item: User): boolean {
    const term = this.searchTerm.toLowerCase();
    return item.email.toLowerCase().includes(term) || item.name.toLowerCase().includes(term);
  }

  protected onItemDeleted(id: number | string): void {
    // Placeholder for post-delete logic (API call, notification, etc)
  }

  ngOnInit(): void {
    this.loadItems();
  }

  protected loadItems(): void {
    this.applyFilter();
  }

  addUser(): void {
    if (this.form.valid) {
      const newUser: User = {
        id: Math.max(...this.items.map(u => u.id), 0) + 1,
        ...this.form.value,
        status: 'active',
        joinDate: new Date().toISOString().split('T')[0],
      };
      this.items.push(newUser);
      this.applyFilter();
      this.form.reset();
      this.showForm = false;
    }
  }

  toggleUserStatus(id: number): void {
    const user = this.items.find(u => u.id === id);
    if (user) {
      user.status = user.status === 'active' ? 'inactive' : 'active';
    }
  }
}
