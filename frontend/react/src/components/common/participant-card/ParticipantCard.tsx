import CopyButton from "../copy-button/CopyButton";
import InfoButton from "../info-button/InfoButton";
import DeleteButton from "../delete-button/DeleteButton";
import ItemCard from "../item-card/ItemCard";
import { type ParticipantCardProps } from "./types";
import "./ParticipantCard.scss";

const ParticipantCard = ({
  firstName,
  lastName,
  isCurrentUser = false,
  isAdmin = false,
  isCurrentUserAdmin = false,
  adminInfo = "",
  participantLink = "",
  onInfoButtonClick,
  showDeleteButton = false,
  onDeleteButtonClick,
}: ParticipantCardProps) => {
  return (
    <ItemCard title={`${firstName} ${lastName}`} isFocusable>
      <div className="participant-card-info-container">
        {isCurrentUser ? <p className="participant-card-role">You</p> : null}

        {!isCurrentUser && isAdmin ? (
          <p className="participant-card-role">Admin</p>
        ) : null}

        {isCurrentUserAdmin ? (
          <CopyButton
            textToCopy={participantLink}
            iconName="link"
            successMessage="Personal Link is copied!"
            errorMessage="Personal Link was not copied. Try again."
          />
        ) : null}

        {isCurrentUserAdmin && !isAdmin ? (
          <InfoButton withoutToaster onClick={onInfoButtonClick} />
        ) : null}

        {showDeleteButton && onDeleteButtonClick ? (
          <DeleteButton onClick={onDeleteButtonClick} />
        ) : null}

        {!isCurrentUser && isAdmin ? (
          <InfoButton infoMessage={adminInfo} />
        ) : null}
      </div>
    </ItemCard>
  );
};

export default ParticipantCard;
