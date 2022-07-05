#java -agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=1044 -jar tika-server-1.25.jar

curl -T "Wind Energy 101.pdf" https://{{config.name}}tikaserver.azurewebsites.net/unpack --header @tikaheaders.txt > windenergy.zip

curl -T "Wind Energy 101.pdf" https://{{config.name}}tikaserver.azurewebsites.net/meta --header @tikaheaders.txt --header "Accept: application/json" > windenergy.meta.json
curl -T "Wind Energy 101.pdf" https://{{config.name}}tikaserver.azurewebsites.net/rmeta --header @tikaheaders.txt --header "Accept: application/json" > windenergy.rmeta.json

curl -T "Wind Energy 101.pdf" https://{{config.name}}tikaserver.azurewebsites.net/tika --header @tikaheaders.txt --header "Accept: text/html" > swindenergy.html
curl -T "Wind Energy 101.pdf" https://{{config.name}}tikaserver.azurewebsites.net/tika/plain --header @tikaheaders.txt --header "Accept: text/plain" > swindenergy.text
