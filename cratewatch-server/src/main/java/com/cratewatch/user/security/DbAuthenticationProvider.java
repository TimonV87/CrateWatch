package com.cratewatch.user.security;

import com.cratewatch.user.domain.User;
import com.cratewatch.user.repositories.UserRepository;
import io.micronaut.http.HttpRequest;
import io.micronaut.security.authentication.AuthenticationRequest;
import io.micronaut.security.authentication.AuthenticationResponse;
import io.micronaut.security.authentication.provider.HttpRequestAuthenticationProvider;
import jakarta.inject.Singleton;
import lombok.AllArgsConstructor;
import org.mindrot.jbcrypt.BCrypt;

import java.util.Optional;

@AllArgsConstructor
@Singleton
public class DbAuthenticationProvider implements HttpRequestAuthenticationProvider<Object> {

    private final UserRepository userRepository;

    @Override
    public AuthenticationResponse authenticate(HttpRequest<Object> requestContext,
                                                AuthenticationRequest<String, String> authRequest) {
        String username = authRequest.getIdentity();
        String password = authRequest.getSecret();

        Optional<User> user = userRepository.findByUsername(username);
        if (user.isPresent() && BCrypt.checkpw(password, user.get().getPasswordHash())) {
            return AuthenticationResponse.success(username);
        }
        return AuthenticationResponse.failure("Invalid username or password");
    }
}
