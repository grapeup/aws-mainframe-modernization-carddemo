package com.grapeup.carddemo.onlinetransactionmanagement.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Embeddable;

/**
 * Value object: these fields are always used together, so they are one
 * concept here.
 * <p>Merchant ID, name, city, and ZIP are a coherent real-world concept (the merchant where the transaction occurred). They are always used together and map naturally to a value object. Embedded, not a separate table, because there is no independent merchant lifecycle in the legacy system.</p>
 */
@Embeddable
public class Merchant {

    /** Merchant identifier. Nullable for transactions like online bill payments that have no physical merchant. */
    @Column(name = "merchant_id")
    private Integer id;

    @Column(name = "merchant_name", length = 50)
    private String name;

    @Column(name = "merchant_city", length = 50)
    private String city;

    /** ZIP/postal code - String to preserve leading zeros */
    @Column(name = "merchant_zip", length = 10)
    private String zip;

    public Integer getId() {
        return id;
    }

    public void setId(Integer id) {
        this.id = id;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getCity() {
        return city;
    }

    public void setCity(String city) {
        this.city = city;
    }

    public String getZip() {
        return zip;
    }

    public void setZip(String zip) {
        this.zip = zip;
    }
}