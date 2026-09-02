package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import org.springframework.boot.SpringBootConfiguration;
import org.springframework.boot.autoconfigure.EnableAutoConfiguration;
import org.springframework.boot.autoconfigure.domain.EntityScan;

/** Test-only context root. Lives under src/test/java, so it never ships. */
@SpringBootConfiguration
@EnableAutoConfiguration
@EntityScan("com.grapeup.carddemo.onlinetransactionmanagement.domain")
class IntegrationTestContext {
}
