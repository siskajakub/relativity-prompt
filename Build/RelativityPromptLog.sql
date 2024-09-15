<script>
	<name>Prompt Log</name>
	<version>1</version>
	<key>ac584caa-dedd-497e-90bb-49c1ed8d1804</key>
	<description>This Relativity script returns AI Prompt log table.</description>
	<category>Prompt</category>
	<input>
		<field id="IdField" name="Document ID Field:">
			<filters>
				<type>0</type>
				<type>1</type>
				<required>true</required>
			</filters>
		</field>
		<field id="LogField" name="AI Prompt Log Field:">
			<filters>
				<type>4</type>
				<required>true</required>
			</filters>
		</field>
		<constant id="Year" name="Log Filter Year (YYYY; 0000 all)" type="number">
			<required>true</required>
		</constant>
		<constant id="Month" name="Log Filter Month (MM; 00 all)" type="number">
			<required>true</required>
		</constant>
	</input>
	<action returns="table" timeout="600">
		<![CDATA[
		--DROP TABLE [#data];
		CREATE TABLE [#data] (
			[id] NVARCHAR(MAX),
			[engine] NVARCHAR(MAX),
			[email] NVARCHAR(MAX),
			[timestamp] DATETIME,
			[model] NVARCHAR(MAX),
			[chars_from] NVARCHAR(MAX),
			[chars_to] NVARCHAR(MAX),
		);

		-- Declare variables
		DECLARE @separator_entry CHAR(1);
		DECLARE @separator_value CHAR(1);
		DECLARE @id NVARCHAR(MAX);
		DECLARE @row NVARCHAR(MAX);
		DECLARE @hlp NVARCHAR(MAX);
		DECLARE @engine NVARCHAR(MAX);
		DECLARE @email NVARCHAR(MAX);
		DECLARE @timestamp NVARCHAR(MAX);
		DECLARE @model NVARCHAR(MAX);
		DECLARE @chars_from NVARCHAR(MAX);
		DECLARE @chars_to NVARCHAR(MAX);

		-- Set separators
		SET @separator_entry = CHAR(10);
		SET @separator_value = CHAR(59);

		-- Loop through the entries and parse log to nice table
		DECLARE [a] CURSOR FAST_FORWARD FOR
			SELECT
				[#IdField#],
				[#LogField#]
			FROM [EDDSDBO].[Document]
			WHERE LEN([#LogField#]) > 0;

		OPEN [a];
		FETCH NEXT FROM [a] INTO @id, @row;
		WHILE @@FETCH_STATUS = 0
		BEGIN
			SET @row = REPLACE(@row, CHAR(13), '');
			WHILE LEN(@row) > 0
			BEGIN
				-- Select till new line
				SET @hlp = LEFT(@row, CHARINDEX(@separator_entry, @row) - 1);
				-- Adjust value being parsed
				SET @row = SUBSTRING(@row, CHARINDEX(@separator_entry, @row) + 1, LEN(@row));
				
				SET @engine = LEFT(@hlp, CHARINDEX(@separator_value, @hlp) - 1);
				SET @hlp = RIGHT(@hlp, LEN(@hlp) - CHARINDEX(@separator_value, @hlp));
				--PRINT @engine;

				SET @email = LEFT(@hlp, CHARINDEX(@separator_value, @hlp) - 1);
				SET @hlp = RIGHT(@hlp, LEN(@hlp) - CHARINDEX(@separator_value, @hlp));
				--PRINT @email;

				SET @timestamp = LEFT(@hlp, CHARINDEX(@separator_value, @hlp) - 1);
				SET @hlp = RIGHT(@hlp, LEN(@hlp) - CHARINDEX(@separator_value, @hlp));
				--PRINT @timestamp;

				SET @model = LEFT(@hlp, CHARINDEX(@separator_value, @hlp) - 1);
				SET @hlp = RIGHT(@hlp, LEN(@hlp) - CHARINDEX(@separator_value, @hlp));
				--PRINT @model;

				SET @chars_from = LEFT(@hlp, CHARINDEX(@separator_value, @hlp) - 1);
				SET @hlp = RIGHT(@hlp, LEN(@hlp) - CHARINDEX(@separator_value, @hlp));
				--PRINT @chars_from;

				SET @chars_to = @hlp;
				--PRINT @chars_to;
				
				IF (#Month# = '00' OR #Month# = FORMAT(CAST(@timestamp AS DATETIME), 'MM')) AND (#Year# = '0000' OR #Year# = FORMAT(CAST(@timestamp AS DATETIME), 'yyyy'))
					INSERT INTO [#data] ([id], [engine], [email], [timestamp], [model], [chars_from], [chars_to]) VALUES (@id, @engine, @email, @timestamp, @model, @chars_from, @chars_to);

			END;
			FETCH NEXT FROM [a] INTO @id, @row;
		END;
		CLOSE [a];
		DEALLOCATE [a];

		-- Get results
		SELECT * FROM [#data] ORDER BY [timestamp], [id];
		]]>
	</action>
</script>