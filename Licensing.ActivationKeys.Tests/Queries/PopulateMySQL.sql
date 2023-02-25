INSERT INTO [Licenses] ([Key],
                        [Expiration],
                        [Type],
                        [UsingDevices],
                        [MaxDevices])
VALUES ('AAAAA-AAAAA-AAAAA-AAAAA-AAAAA',
        ADDDATE(CURDATE(), INTERVAL 1 YEAR),
        'Professional',
        0,
        3);

INSERT INTO [Licenses] ([Key],
                        [Expiration],
                        [Type],
                        [UsingDevices],
                        [MaxDevices])
VALUES ('BBBBB-BBBBB-BBBBB-BBBBB-BBBBB',
        ADDDATE(CURDATE(), -15),
        'General',
        0,
        1);

INSERT INTO [Licenses] ([Key],
                        [Expiration],
                        [Type],
                        [UsingDevices],
                        [MaxDevices])
VALUES ('CCCCC-CCCCC-CCCCC-CCCCC-CCCCC',
        ADDDATE(CURDATE(), -3),
        'General',
        0,
        1);
