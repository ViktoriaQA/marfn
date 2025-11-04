import type { GetParticipantsResponse } from "../../../types/api";

export interface ParticipantsListProps {
  participants: GetParticipantsResponse;
  userCode?: string;
  onParticipantDeleted?: () => void;
}

export interface PersonalInformation {
  firstName: string;
  lastName: string;
  phone: string;
  email?: string;
  deliveryInfo: string;
  link?: string;
}
