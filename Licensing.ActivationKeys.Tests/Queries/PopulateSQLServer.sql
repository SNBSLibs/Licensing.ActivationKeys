INSERT INTO [Licenses] ([Key],
                        [Expiration],
                        [Type],
                        [UsingDevices],
                        [MaxDevices])
VALUES ('AAAAA-AAAAA-AAAAA-AAAAA-AAAAA',
        DATEADD(YEAR, 1, GETDATE()),
        'Professional',
        0,
        3);

INSERT INTO [Licenses] ([Key],
                        [Expiration],
                        [Type],
                        [UsingDevices],
                        [MaxDevices])
VALUES ('BBBBB-BBBBB-BBBBB-BBBBB-BBBBB',
        DATEADD(DAY, -15, GETDATE()),
        'General',
        0,
        1);

INSERT INTO [Licenses] ([Key],
                        [Expiration],
                        [Type],
                        [UsingDevices],
                        [MaxDevices])
VALUES ('CCCCC-CCCCC-CCCCC-CCCCC-CCCCC',
        DATEADD(DAY, -3, GETDATE()),
        'General',
        0,
        1);
