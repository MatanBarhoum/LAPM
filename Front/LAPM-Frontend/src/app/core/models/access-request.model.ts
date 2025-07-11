export interface AccessRequest {
    id: number;
    computerName: string;
    domainUser: string;
    requestor: string;
    requestTime: Date;
    approvalTime?: Date;
    approver?: string;
    expirationTime: Date;
    status: 'Pending' | 'Approved' | 'Rejected' | 'Applied' | 'Expired' | 'Revoked';
    notes?: string;
  }