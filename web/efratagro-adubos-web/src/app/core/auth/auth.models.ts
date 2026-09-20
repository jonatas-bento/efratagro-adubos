export const applicationRoles = {
  admin: 'Admin',
  manager: 'Manager',
  seller: 'Seller',
} as const;

export type ApplicationRole =
  typeof applicationRoles[
    keyof typeof applicationRoles
  ];

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  tokenType: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  name: string;
  roles: string[];
}
