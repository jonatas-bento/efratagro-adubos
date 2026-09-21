export interface SystemUser {
  id: string;
  displayName: string;
  email: string;
  role: string;
  isActive: boolean;
  isLockedOut: boolean;
  accessFailedCount: number;
  lockoutEnd: string | null;
}

export interface CreateSystemUserRequest {
  displayName: string;
  email: string;
  password: string;
  role: string;
}

export interface UpdateSystemUserRequest {
  displayName: string;
  email: string;
  role: string;
  isActive: boolean;
}

export interface ResetUserPasswordRequest {
  newPassword: string;
}
