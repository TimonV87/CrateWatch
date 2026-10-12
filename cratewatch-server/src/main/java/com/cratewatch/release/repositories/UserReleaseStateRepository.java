package com.cratewatch.release.repositories;

import com.cratewatch.release.domain.UserReleaseState;
import io.micronaut.data.jdbc.annotation.JdbcRepository;
import io.micronaut.data.model.query.builder.sql.Dialect;
import io.micronaut.data.repository.CrudRepository;

import java.util.List;
import java.util.UUID;

@JdbcRepository(dialect = Dialect.POSTGRES)
public interface UserReleaseStateRepository extends CrudRepository<UserReleaseState, UUID> {

    List<UserReleaseState> findByUserIdAndCdReleaseIdIn(UUID userId, List<UUID> cdReleaseIds);
}
