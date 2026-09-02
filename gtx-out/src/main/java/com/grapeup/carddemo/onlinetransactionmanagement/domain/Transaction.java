package com.grapeup.carddemo.onlinetransactionmanagement.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Embedded;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.math.BigDecimal;
import java.time.OffsetDateTime;

/** A financial transaction (payment, purchase, etc.) against a card, with merchant details and timestamps */
@Entity
@Table(name = "transactions")
public class Transaction {

    /** Kept as String because the legacy system treats it as a character field. */
    @Id
    @Column(name = "id", length = 16)
    private String id;

    /** Two-character code (e.g. '02' for payment). String because leading zero is significant. */
    @Column(name = "type_code", length = 2)
    private String typeCode;

    /** Numeric category (e.g. 2 for bill payment) */
    @Column(name = "category_code")
    private Short categoryCode;

    /** Origin channel, e.g. 'POS TERM' */
    @Column(name = "source", length = 10)
    private String source;

    /** Human-readable description, e.g. 'BILL PAYMENT - ONLINE' */
    @Column(name = "description", length = 100)
    private String description;

    /** Money - BigDecimal, never double. For bill payment this equals the account's current balance. */
    @Column(name = "amount", precision = 11, scale = 2)
    private BigDecimal amount;

    /** Proven timestamp field. When the transaction was initiated. */
    @Column(name = "originated_at")
    private OffsetDateTime originatedAt;

    /** Proven timestamp field. When the transaction was processed/settled. */
    @Column(name = "processed_at")
    private OffsetDateTime processedAt;

    /** Each transaction is charged to a specific card. Account is reached via the multi-hop route through card_cross_references, NOT by a direct FK. */
    @Column(name = "card_number", length = 16)
    private String cardNumber;

    /** Merchant ID, name, city, and ZIP are a coherent real-world concept (the merchant where the transaction occurred). They are always used together and map naturally to a value object. Embedded, not a separate table, because there is no independent merchant lifecycle in the legacy system. */
    @Embedded
    private Merchant merchant;

    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getTypeCode() {
        return typeCode;
    }

    public void setTypeCode(String typeCode) {
        this.typeCode = typeCode;
    }

    public Short getCategoryCode() {
        return categoryCode;
    }

    public void setCategoryCode(Short categoryCode) {
        this.categoryCode = categoryCode;
    }

    public String getSource() {
        return source;
    }

    public void setSource(String source) {
        this.source = source;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }

    public BigDecimal getAmount() {
        return amount;
    }

    public void setAmount(BigDecimal amount) {
        this.amount = amount;
    }

    public OffsetDateTime getOriginatedAt() {
        return originatedAt;
    }

    public void setOriginatedAt(OffsetDateTime originatedAt) {
        this.originatedAt = originatedAt;
    }

    public OffsetDateTime getProcessedAt() {
        return processedAt;
    }

    public void setProcessedAt(OffsetDateTime processedAt) {
        this.processedAt = processedAt;
    }

    public String getCardNumber() {
        return cardNumber;
    }

    public void setCardNumber(String cardNumber) {
        this.cardNumber = cardNumber;
    }

    public Merchant getMerchant() {
        return merchant;
    }

    public void setMerchant(Merchant merchant) {
        this.merchant = merchant;
    }
}