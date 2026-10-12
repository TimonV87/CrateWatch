package com.cratewatch.release.domain;

import io.micronaut.data.annotation.GeneratedValue;
import io.micronaut.data.annotation.Id;
import io.micronaut.data.annotation.MappedEntity;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

@Getter
@Setter
@MappedEntity("user_release_states")
public class UserReleaseState {

    @Id
    @GeneratedValue
    private UUID id;

    private UUID userId;

    private UUID cdReleaseId;

    private String state;

    public UserReleaseState() {
    }

    public UserReleaseState(UUID userId, UUID cdReleaseId) {
        this.userId = userId;
        this.cdReleaseId = cdReleaseId;
        this.state = "NONE";
    }
}
